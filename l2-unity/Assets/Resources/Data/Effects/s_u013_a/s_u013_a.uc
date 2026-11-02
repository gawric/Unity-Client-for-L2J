class s_u013_a extends NCubics;	//큐빅(윈드스트라이크)

defaultproperties
{
     Begin Object Class=SpriteEmitter Name=SpriteEmitter11
         ColorScale(0)=(Color=(B=255,G=255,R=255,A=255))
         ColorScale(1)=(RelativeTime=1.000000,Color=(B=255,G=255,R=255,A=255))
         Opacity=0.450000
         OpacityRatio=1.000000
         FadeOutStartTime=10.000000
         MaxParticles=1
         Name="SpriteEmitter8"
         UseSizeScale=True
         UseRegularSizeScale=False
         SizeScale(0)=(RelativeTime=0.460000,RelativeSize=0.950000)
         SizeScale(1)=(RelativeTime=1.000000,RelativeSize=1.000000)
         SizeScaleRepeats=79.000000
         StartSizeRange=(X=(Min=4.000000,Max=4.000000),Y=(Min=4.000000,Max=4.000000),Z=(Min=4.000000,Max=4.000000))
         UniformSize=True
         InitialParticlesPerSecond=100000.000000
         AutomaticInitialSpawning=False
         Texture=Texture'LineageEffectsTextures.Cubics.fx_m_t0091'
         TextureUSubdivisions=4
         TextureVSubdivisions=4
         SubdivisionStart=3
         SubdivisionEnd=4
         UseRandomSubdivision=True
         LifetimeRange=(Min=10.000000,Max=10.000000)
         Name="SpriteEmitter11"
     End Object
     Emitters(0)=SpriteEmitter'lineageeffect.s_u013_a.SpriteEmitter11'
     Begin Object Class=MeshEmitter Name=MeshEmitter5
         StaticMesh=StaticMesh'LineageEffectsStaticmeshes.Cubics.cubic_wind00'
         UseMeshBlendMode=False
         RenderTwoSided=True
         ColorScale(0)=(Color=(B=255,G=255,R=255,A=255))
         ColorScale(1)=(RelativeTime=0.314286,Color=(B=194,G=194,R=194,A=255))
         ColorScale(2)=(RelativeTime=1.000000,Color=(B=255,G=255,R=255,A=255))
         ColorScaleRepeats=70.000000
         OpacityRatio=1.000000
         FadeOutStartTime=10.000000
         MaxParticles=1
         Name="MeshEmitter9"
         ZWrite=True
         SpinParticles=True
         SpinCCWorCW=(X=0.000000)
         SpinsPerSecondRange=(X=(Min=3.000000,Max=3.000000))
         StartSizeRange=(X=(Min=0.018000,Max=0.018000),Y=(Min=0.018000,Max=0.018000),Z=(Min=0.018000,Max=0.018000))
         InitialParticlesPerSecond=100000.000000
         AutomaticInitialSpawning=False
         DrawStyle=PTDS_AlphaBlend
         LifetimeRange=(Min=10.000000,Max=10.000000)
         Name="MeshEmitter5"
     End Object
     Emitters(1)=MeshEmitter'lineageeffect.s_u013_a.MeshEmitter5'
     AutoReset=True
     Tag="Emitter"
     DrawScale=0.010000
     bUnlit=False
}
