using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "VfxDatabase", menuName = "Mugen/Vfx Database")]
public class MugenVfxDatabase : ScriptableObject
{
    public enum PosType { P1, P2, Front, Back, Left, Right }

    [System.Serializable]
    public class VfxProfile
    {
        public string id;
        public Vector3 scale = Vector3.one;
        public Vector3 offset = Vector3.zero;
        public Vector3 velocity = Vector3.zero;
        public PosType posType = PosType.P1; // Crucial for Cutscenes
        public int sprPriority = 0;          // Crucial for Layering
        public int bindTime = 0;             // Crucial for Following
        public int facing = 1;               // Crucial for P2 side
        public bool isAdditive = false;
    }

    [System.Serializable]
    public class StateMapping
    {
        public string stateID;
        public string defaultAnimID;
    }

    public List<VfxProfile> profiles = new List<VfxProfile>();
    public List<StateMapping> stateMappings = new List<StateMapping>();

    public VfxProfile GetProfile(string searchID) => profiles.Find(x => x.id == searchID);
    public string GetAnimForState(string stateID)
    {
        var map = stateMappings.Find(x => x.stateID == stateID);
        return map != null ? map.defaultAnimID : stateID;
    }
}