using UnityEngine;
using UnityEngine.UI;

namespace HexMap.UI
{
    public class SaveLoadItem : MonoBehaviour
    {
        private string mapName;

        public SaveLoadMenu menu;

        public string MapName
        {
            get { return mapName; }
            set
            {
                mapName = value;
                transform.GetChild(index: 0).GetComponent<Text>().text = value;
            }
        }

        public void Select()
        {
            menu.SelectItem(name: mapName);
        }
    }
}