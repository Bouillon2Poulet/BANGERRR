using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SolPuzzlePiece : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        NPCEventsManager M = FindObjectOfType<NPCEventsManager>();
        if (!M.Isador_PuzzlePieceFound)
        {
            M.Isador_PuzzlePieceFound = true;
            M.updateNPCPages();
            string message = "Vous avez trouv� une pi�ce de puzzle ! Elle est minuscule !";
            FindObjectOfType<DialogManager>().OpenMessage(message, "Objet trouv�");
            enabled = false;
        }
    }

    public Transform GetTransform()
    {
        return transform;
    }
}
