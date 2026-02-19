using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct  PlayerInfoInterlude
{
      public NetworkIdentityInterlude Identity { get; set; }
      public PlayerStatusInterlude Status { get; set; }
      public PlayerInterludeStats Stats { get; set; }
      public PlayerInterludeAppearance Appearance { get; set; }
      public CharacterSkills Skills { get; set; }
}
