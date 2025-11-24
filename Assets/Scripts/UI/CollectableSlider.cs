using UnityEngine;

public class CollectableSlider : MonoBehaviour
{
    [SerializeField] private float maxValue;
    [SerializeField] private float minValue;
    [SerializeField] private int divisionCount;
    [SerializeField] private GameObject[] divisions;
    private int currentDivision;

    public void UpdateSlider(float value)
    {
        currentDivision = (int)(value / (maxValue / divisionCount));
        for (int i = 0; i < divisionCount; i++)
            divisions[i].SetActive(i > currentDivision);
    }
}