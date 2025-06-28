
using NamespaceFactoryHumans;
using NamespaceFactoryTowers;

namespace NamespaceWorld
{
    [System.Serializable]
    public class WorldScore
    {
        public HumanData[] humansCoeffsByType;
        public float[] towerCoeffsByType;

        public float score;

        public void ScoreReset() { score = 0; }
        public void ScoreSet(float score) { this.score = score; }
        public float ScoreGet() => score;

        public void ScoreAddHuman(HumanType type, HumanData data)
        {
            score += data.Scalar(humansCoeffsByType[(int)type]);
        }

        public void ScoreAddTower(TowerType type)
        {
            score += towerCoeffsByType[(int)type];
        }
    }
}