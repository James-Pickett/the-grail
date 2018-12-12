using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace HexMap.UI
{
    public class SaveLoadMenu : MonoBehaviour
    {
        private const int mapFileVersion = 5;

        public HexGrid hexGrid;

        public SaveLoadItem itemPrefab;

        public RectTransform listContent;

        public Text menuLabel, actionButtonLabel;

        public InputField nameInput;

        private bool saveMode;

        public void Open(bool saveMode)
        {
            this.saveMode = saveMode;
            if (saveMode)
            {
                menuLabel.text = "Save Map";
                actionButtonLabel.text = "Save";
            }
            else
            {
                menuLabel.text = "Load Map";
                actionButtonLabel.text = "Load";
            }

            FillList();
            gameObject.SetActive(value: true);
            HexMapCamera.Locked = true;
        }

        public void Close()
        {
            gameObject.SetActive(value: false);
            HexMapCamera.Locked = false;
        }

        public void Action()
        {
            var path = GetSelectedPath();
            if (path == null)
            {
                return;
            }

            if (saveMode)
            {
                Save(path: path);
            }
            else
            {
                Load(path: path);
            }

            Close();
        }

        public void SelectItem(string name)
        {
            nameInput.text = name;
        }

        public void Delete()
        {
            var path = GetSelectedPath();
            if (path == null)
            {
                return;
            }

            if (File.Exists(path: path))
            {
                File.Delete(path: path);
            }

            nameInput.text = "";
            FillList();
        }

        private void FillList()
        {
            for (var i = 0; i < listContent.childCount; i++)
            {
                Destroy(obj: listContent.GetChild(index: i).gameObject);
            }

            var paths =
                Directory.GetFiles(path: Application.persistentDataPath, searchPattern: "*.map");
            Array.Sort(array: paths);
            for (var i = 0; i < paths.Length; i++)
            {
                var item = Instantiate(original: itemPrefab);
                item.menu = this;
                item.MapName = Path.GetFileNameWithoutExtension(path: paths[i]);
                item.transform.SetParent(parent: listContent, worldPositionStays: false);
            }
        }

        private string GetSelectedPath()
        {
            var mapName = nameInput.text;
            if (mapName.Length == 0)
            {
                return null;
            }

            return Path.Combine(path1: Application.persistentDataPath, path2: mapName + ".map");
        }

        private void Save(string path)
        {
            using (
                var writer =
                    new BinaryWriter(output: File.Open(path: path, mode: FileMode.Create))
            )
            {
                writer.Write(value: mapFileVersion);
                hexGrid.Save(writer: writer);
            }
        }

        private void Load(string path)
        {
            if (!File.Exists(path: path))
            {
                Debug.LogError(message: "File does not exist " + path);
                return;
            }

            using (var reader = new BinaryReader(input: File.OpenRead(path: path)))
            {
                var header = reader.ReadInt32();
                if (header <= mapFileVersion)
                {
                    hexGrid.Load(reader: reader, header: header);
                    HexMapCamera.ValidatePosition();
                }
                else
                {
                    Debug.LogWarning(message: "Unknown map format " + header);
                }
            }
        }
    }
}