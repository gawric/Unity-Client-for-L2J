NPC deco = npcgrp.deco_effect. One Unreal Emitter class, attached like L2 InitDecoEffect:

  AttachToBone(r_bone1 / r_bone2 / r_bone3), else NPC root.
  Height is StartLocationRange.Z on that one actor (feet glow vs head ring).
  Do not use skill CompositeEffectV2 / _ca cast timing.

Generate (L2Particle only, no composite):
  Tools/L2 Effects/Generate NPC Deco (u_npc_id_buff)
  or Unity -executeMethod L2EffectGeneratorWindow.GenerateNpcDecoCli [-decoName u_npc_id_buff]

Layout:
  Data/Effects/deco/<class>/<class>.uc
  Data/Effects/deco/<class>/<class>.prefab

Optional authored splits (rare): piece name _feet / _ground / _oh / _head.
Skill suffix _ca is NOT feet (ra_boss_halo_a_ca stays on bone).
