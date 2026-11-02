class s_u013_c extends NCubics; // Å¥ºò(Èú)

defaultproperties
{
     Begin Object Class=SpriteEmitter Name=SpriteEmitter2
         ColorScale(0)=(Color=(B=255,G=255,R=255,A=255))
         ColorScale(1)=(RelativeTime=1.000000,Color=(B=255,G=255,R=255,A=255))
         Opacity=0.500000
         OpacityRatio=1.000000
         FadeOutStartTime=10.000000
         MaxParticles=1
         Name="SpriteEmitter8"
         SpinParticles=True
         StartSpinRange=(X=(Min=0.300000,Max=0.300000))
         UseSizeScale=True
         UseRegularSizeScale=False
         SizeScale(0)=(RelativeTime=0.460000,RelativeSize=0.950000)
         SizeScale(1)=(RelativeTime=1.000000,RelativeSize=1.000000)
         SizeScaleRepeats=99.000000
         StartSizeRange=(X=(Min=4.000000,Max=4.000000),Y=(Min=4.000000,Max=4.000000),Z=(Min=4.000000,Max=4.000000))
         UniformSize=True
         InitialParticlesPerSecond=100000.000000
         AutomaticInitialSpawning=False
         Texture=Texture'LineageEffectsTextures.Cubics.fx_m_t0091'
         TextureUSubdivisions=4
         TextureVSubdivisions=4
         SubdivisionEnd=1
         UseRandomSubdivision=True
         LifetimeRange=(Min=10.000000,Max=10.000000)
         Name="SpriteEmitter2"
     End Object
     Emitters(0)=SpriteEmitter'lineageeffect.s_u013_c.SpriteEmitter2'
     Begin Object Class=MeshEmitter Name=MeshEmitter1
         StaticMesh=StaticMesh'LineageEffectsStaticmeshes.Cubics.cubic_heal00'
         UseMeshBlendMode=False
         ColorScale(0)=(Color=(B=255,G=255,R=255,A=255))
         ColorScale(1)=(RelativeTime=0.532143,Color=(B=255,G=255,R=255,A=200))
         ColorScale(2)=(RelativeTime=1.000000,Color=(B=255,G=255,R=255,A=255))
         ColorScaleRepeats=99.000000
         OpacityRatio=1.000000
         FadeOutStartTime=10.000000
         MaxParticles=1
         Name="MeshEmitter9"
         ZWrite=True
         SpinParticles=True
         SpinCCWorCW=(X=0.000000)
         SpinsPerSecondRange=(X=(Min=0.200000,Max=0.200000))
         StartSizeRange=(X=(Min=0.047000,Max=0.047000),Y=(Min=0.047000,Max=0.047000),Z=(Min=0.047000,Max=0.047000))
         UniformSize=True
         InitialParticlesPerSecond=100000.000000
         AutomaticInitialSpawning=False
         DrawStyle=PTDS_AlphaBlend
         LifetimeRange=(Min=10.000000,Max=10.000000)
         Name="MeshEmitter1"
     End Object
     Emitters(1)=MeshEmitter'lineageeffect.s_u013_c.MeshEmitter1'
     AutoReset=True
     Tag="Emitter"
     DrawScale=0.010000
     bUnlit=False
}
