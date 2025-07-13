using System.Collections.Concurrent;
using System;
using System.Linq;

namespace AutoEmotion.ChatMessages
{
    public class ReactionCache
    {

        private readonly int maxActions;
        private readonly int timeoutSeconds;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<uint, (DateTime LastTime, int Count)>> cache;

        public ReactionCache(int maxActions = 2, int timeoutSeconds = 5)
        {
            this.maxActions = maxActions;
            this.timeoutSeconds = timeoutSeconds;
            cache = new ConcurrentDictionary<string, ConcurrentDictionary<uint, (DateTime, int)>>();
        }

        public bool CanPerformAction(string userID, uint emoteID)
        {
            var now = DateTime.UtcNow;

            // Ottiene o crea il dizionario delle azioni per l'utente
            var userActions = cache.GetOrAdd(userID,
                _ => new ConcurrentDictionary<uint, (DateTime, int)>());

            // Se l'utente non ha mai fatto questa azione
            if (!userActions.TryGetValue(emoteID, out var emoteInfo))
            {
                return true;
            }

            var (lastTime, count) = emoteInfo;

            // Verifica se il timeout è scaduto
            if ((now - lastTime).TotalSeconds > timeoutSeconds)
            {
                return true;
            }

            // Verifica se ha raggiunto il limite massimo di azioni
            return count < maxActions;
        }

        public bool RecordAction(string userID, uint emoteID)
        {
            var now = DateTime.UtcNow;
            var userActions = cache.GetOrAdd(userID,
                _ => new ConcurrentDictionary<uint, (DateTime, int)>());

            userActions.AddOrUpdate(
                emoteID,
                _ => (now, 1),
                (_, existing) =>
                {
                    if ((now - existing.LastTime).TotalSeconds > timeoutSeconds)
                    {
                        return (now, 1);
                    }
                    return (existing.LastTime, existing.Count + 1);
                });

            return true;
        }

        public void CleanExpiredActions()
        {
            var now = DateTime.UtcNow;

            foreach (var userPair in cache)
            {
                var userActions = userPair.Value;
                var expiredActions = userActions
                    .Where(action => (now - action.Value.LastTime).TotalSeconds > timeoutSeconds)
                    .Select(action => action.Key)
                    .ToList();

                foreach (var action in expiredActions)
                {
                    userActions.TryRemove(action, out _);
                }

                // Rimuovi l'utente se non ha più azioni
                if (userActions.IsEmpty)
                {
                    cache.TryRemove(userPair.Key, out _);
                }
            }
        }

#if DEBUG
        public int GetTotalCacheKeys()
        {
            return cache.Sum(user => user.Value.Count);
        }
#endif

    }
}
