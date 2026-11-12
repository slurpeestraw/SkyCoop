using System;
using System.Collections.Generic;
using System.Numerics;

namespace SkyCoopServer
{
    [Flags]
    public enum EntityFlags
    {
        None = 0,
        Final = 1,
    }

    public enum UpdateDecision
    {
        Reject,
        Accept,
        AcceptAndRemove,
    }

    public interface IEntityPolicy
    {
        string Kind { get; }
        bool AcceptsUnlockedFinalReports { get; }
    }

    public class ReplicatedEntity
    {
        public string m_Kind = "";
        public string m_GUID = "";
        public string m_Prefab = "";
        public string m_State = "";
        public Vector3 m_Position;
        public Quaternion m_Rotation;
        public int m_Uploader = -1;
        public DateTime m_PendingSinceUtc;

        public bool IsPending => string.IsNullOrEmpty(m_State);

        public class SaveData
        {
            public string Kind { get; set; } = "";
            public string GUID { get; set; } = "";
            public string Prefab { get; set; } = "";
            public string State { get; set; } = "";
            public DataStr.Vector3JSON Position { get; set; } = new DataStr.Vector3JSON();
            public DataStr.QuaternionJSON Rotation { get; set; } = new DataStr.QuaternionJSON();
        }

        public SaveData Save()
        {
            return new SaveData
            {
                Kind = m_Kind,
                GUID = m_GUID,
                Prefab = m_Prefab,
                State = m_State,
                Position = new DataStr.Vector3JSON(m_Position.X, m_Position.Y, m_Position.Z),
                Rotation = new DataStr.QuaternionJSON(m_Rotation.X, m_Rotation.Y, m_Rotation.Z, m_Rotation.W),
            };
        }

        public static ReplicatedEntity Load(SaveData data)
        {
            return new ReplicatedEntity
            {
                m_Kind = data.Kind ?? "",
                m_GUID = data.GUID ?? "",
                m_Prefab = data.Prefab ?? "",
                m_State = data.State ?? "",
                m_Position = data.Position != null ? data.Position.ToVector() : default,
                m_Rotation = data.Rotation != null ? data.Rotation.ToQuaternion() : default,
            };
        }
    }

    public static class EntityRules
    {
        private sealed class DefaultPolicy : IEntityPolicy
        {
            public string Kind => "";
            public bool AcceptsUnlockedFinalReports => false;
        }

        private static readonly Dictionary<string, IEntityPolicy> s_Policies =
            new Dictionary<string, IEntityPolicy>(StringComparer.Ordinal);

        private static readonly IEntityPolicy s_DefaultPolicy = new DefaultPolicy();

        public static void Register(IEntityPolicy policy)
        {
            if (policy == null || string.IsNullOrEmpty(policy.Kind))
            {
                return;
            }

            lock (s_Policies)
            {
                s_Policies[policy.Kind] = policy;
            }
        }

        public static IEntityPolicy PolicyFor(string kind)
        {
            if (!string.IsNullOrEmpty(kind))
            {
                lock (s_Policies)
                {
                    if (s_Policies.TryGetValue(kind, out IEntityPolicy? policy))
                    {
                        return policy;
                    }
                }
            }

            return s_DefaultPolicy;
        }

        public static UpdateDecision Evaluate(
            ReplicatedEntity? existing,
            string senderScene,
            string entityScene,
            int senderId,
            int? lockHolderId,
            EntityFlags flags)
        {
            if (existing == null || senderScene != entityScene)
            {
                return UpdateDecision.Reject;
            }

            bool accepted;
            if (existing.IsPending && senderId == existing.m_Uploader)
            {
                accepted = true;
            }
            else if (lockHolderId == senderId)
            {
                accepted = true;
            }
            else if (flags.HasFlag(EntityFlags.Final) &&
                     lockHolderId == null &&
                     PolicyFor(existing.m_Kind).AcceptsUnlockedFinalReports)
            {
                accepted = true;
            }
            else
            {
                accepted = false;
            }

            if (!accepted)
            {
                return UpdateDecision.Reject;
            }

            return flags.HasFlag(EntityFlags.Final) ? UpdateDecision.AcceptAndRemove : UpdateDecision.Accept;
        }

        public static int? PickUploader(
            ReplicatedEntity entity,
            IReadOnlyList<int> playersInScene,
            DateTime nowUtc,
            TimeSpan timeout)
        {
            if (entity == null || !entity.IsPending)
            {
                return null;
            }

            IReadOnlyList<int> players = playersInScene ?? Array.Empty<int>();

            bool uploaderInScene = false;
            int? lowestOther = null;

            foreach (int playerId in players)
            {
                if (playerId == entity.m_Uploader)
                {
                    uploaderInScene = true;
                    continue;
                }

                if (lowestOther == null || playerId < lowestOther.Value)
                {
                    lowestOther = playerId;
                }
            }

            if (uploaderInScene && nowUtc - entity.m_PendingSinceUtc < timeout)
            {
                return null;
            }

            if (lowestOther != null)
            {
                return lowestOther;
            }

            if (uploaderInScene)
            {
                return entity.m_Uploader;
            }

            return null;
        }
    }
}
