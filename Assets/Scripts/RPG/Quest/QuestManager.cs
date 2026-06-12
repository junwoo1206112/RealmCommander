using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealmCommander.RPG
{
    [Serializable]
    public class QuestData
    {
        public string questId;
        public string questName;
        public string description;
        public QuestType questType;

        public List<QuestObjective> objectives = new List<QuestObjective>();
        public QuestReward reward;

        public bool isCompleted;
        public bool isTurnedIn;

        public float ProgressPercent
        {
            get
            {
                if (objectives.Count == 0) return 0;
                int completed = 0;
                foreach (var obj in objectives)
                {
                    if (obj.IsCompleted) completed++;
                }
                return (float)completed / objectives.Count;
            }
        }

        public bool IsAllObjectivesComplete
        {
            get
            {
                foreach (var obj in objectives)
                {
                    if (!obj.IsCompleted) return false;
                }
                return true;
            }
        }
    }

    [Serializable]
    public class QuestObjective
    {
        public string description;
        public QuestObjectiveType objectiveType;
        public string targetId;
        public int requiredCount;
        public int currentCount;

        public bool IsCompleted => currentCount >= requiredCount;
        public float ProgressPercent => requiredCount > 0 ? (float)currentCount / requiredCount : 1f;
    }

    [Serializable]
    public class QuestReward
    {
        public float goldReward;
        public float expReward;
        public List<ItemData> itemRewards = new List<ItemData>();
    }

    public enum QuestType
    {
        Main,
        Side,
        Daily,
        Event
    }

    public enum QuestObjectiveType
    {
        KillEnemy,
        CollectItem,
        ReachLocation,
        TalkToNPC,
        UseSkill
    }

    [AddComponentMenu("Realm Commander/Prototype/Quest Manager")]
    public class QuestManager : MonoBehaviour
    {
        public const string ScopeLabel = "Prototype - not part of the verified 1v1 gameplay loop";
        public static QuestManager Instance { get; private set; }

        [Header("Quest Settings")]
        [SerializeField] private List<QuestData> availableQuests = new List<QuestData>();
        [SerializeField] private List<QuestData> activeQuests = new List<QuestData>();
        [SerializeField] private List<QuestData> completedQuests = new List<QuestData>();

        public IReadOnlyList<QuestData> ActiveQuests => activeQuests;
        public IReadOnlyList<QuestData> CompletedQuests => completedQuests;

        public event Action<QuestData> OnQuestAccepted;
        public event Action<QuestData> OnQuestCompleted;
        public event Action<QuestData> OnQuestTurnedIn;
        public event Action<QuestData, QuestObjective> OnObjectiveUpdated;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool AcceptQuest(string questId)
        {
            var quest = FindQuestById(questId, availableQuests);
            if (quest == null) return false;

            availableQuests.Remove(quest);
            activeQuests.Add(quest);
            OnQuestAccepted?.Invoke(quest);

            Debug.Log($"Quest accepted: {quest.questName}");
            return true;
        }

        public void UpdateObjectiveProgress(QuestObjectiveType type, string targetId, int amount = 1)
        {
            if (amount <= 0) return;
            var questsToComplete = new List<QuestData>();
            foreach (var quest in activeQuests)
            {
                if (quest.isCompleted) continue;

                foreach (var objective in quest.objectives)
                {
                    if (objective.objectiveType == type && objective.targetId == targetId)
                    {
                        objective.currentCount = Mathf.Min(objective.requiredCount, objective.currentCount + amount);
                        OnObjectiveUpdated?.Invoke(quest, objective);

                        if (quest.IsAllObjectivesComplete && !quest.isCompleted)
                            questsToComplete.Add(quest);
                    }
                }
            }

            foreach (QuestData quest in questsToComplete)
                CompleteQuest(quest);
        }

        private void CompleteQuest(QuestData quest)
        {
            quest.isCompleted = true;
            activeQuests.Remove(quest);
            completedQuests.Add(quest);
            OnQuestCompleted?.Invoke(quest);

            Debug.Log($"Quest completed: {quest.questName}");
        }

        public QuestReward TurnInQuest(string questId)
        {
            var quest = FindQuestById(questId, completedQuests);
            if (quest == null || quest.isTurnedIn || quest.reward == null) return null;

            quest.isTurnedIn = true;

            if (Mirror.NetworkServer.active && RTS.ResourceManager.Instance != null)
            {
                RTS.ResourceManager.Instance.AddGold(quest.reward.goldReward);
            }

            OnQuestTurnedIn?.Invoke(quest);
            return quest.reward;
        }

        private QuestData FindQuestById(string questId, List<QuestData> questList)
        {
            foreach (var quest in questList)
            {
                if (quest.questId == questId) return quest;
            }
            return null;
        }

        public QuestData GetActiveQuest(string questId)
        {
            return FindQuestById(questId, activeQuests);
        }
    }
}
