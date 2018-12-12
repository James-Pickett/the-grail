using UnityEngine;

namespace HexMap.UI
{
    public class NewMapMenu : MonoBehaviour
    {
        private bool generateMaps = true;

        public HexGrid hexGrid;

        public HexMapGenerator mapGenerator;

        private bool wrapping = true;

        public void ToggleMapGeneration(bool toggle)
        {
            generateMaps = toggle;
        }

        public void ToggleWrapping(bool toggle)
        {
            wrapping = toggle;
        }

        public void Open()
        {
            gameObject.SetActive(value: true);
            HexMapCamera.Locked = true;
        }

        public void Close()
        {
            gameObject.SetActive(value: false);
            HexMapCamera.Locked = false;
        }

        public void CreateSmallMap()
        {
            CreateMap(x: 20, z: 15);
        }

        public void CreateMediumMap()
        {
            CreateMap(x: 40, z: 30);
        }

        public void CreateLargeMap()
        {
            CreateMap(x: 80, z: 60);
        }

        private void CreateMap(int x, int z)
        {
            if (generateMaps)
            {
                mapGenerator.GenerateMap(x: x, z: z, wrapping: wrapping);
            }
            else
            {
                hexGrid.CreateMap(x: x, z: z, wrapping: wrapping);
            }

            HexMapCamera.ValidatePosition();
            Close();
        }
    }
}