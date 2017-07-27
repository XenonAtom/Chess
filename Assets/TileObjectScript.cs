using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public class TileObjectScript : MonoBehaviour
    {

        public ChessControllerScript gameController;

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnMouseDown()
        {
            gameController.ButtonCallback(Utils.TileNameToPosition(this.name));
        }
    }
}