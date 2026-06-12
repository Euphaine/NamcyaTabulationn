using System;

namespace NamcyaTabulation.Services
{
    public class ScoreBroadcastService
    {
        public event Action<int>? OnScoreUpdated;

        public void BroadcastScoreUpdate(int categoryId)
        {
            OnScoreUpdated?.Invoke(categoryId);
        }
    }
}