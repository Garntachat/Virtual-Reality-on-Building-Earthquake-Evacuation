using UnityEngine;

namespace ChulaEarthquakeVR
{
    public sealed class TaskItem : MonoBehaviour
    {
        [SerializeField] private string itemId = "item";
        public string ItemId => itemId;
        public void Configure(string id) => itemId = string.IsNullOrWhiteSpace(id) ? name : id;
    }
}
