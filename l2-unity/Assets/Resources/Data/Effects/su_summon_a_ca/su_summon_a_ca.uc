class su_summon_a_ca extends Emitter;

defaultproperties
{
     Begin Object Class=MeshEmitter Name=MeshEmitter4
         StaticMesh=StaticMesh'LineageEffectsStaticmeshes.Summon.summon00'
         UseMeshBlendMode=False
         RenderTwoSided=True
         UseColorScale=True
         ColorScale(0)=(Color=(B=255,G=255,R=255,A=255))
         ColorScale(1)=(RelativeTime=0.571429,Color=(B=226,G=211,R=205,A=255))
         ColorScale(2)=(RelativeTime=1.000000,Color=(B=255,G=255,R=255,A=255))
         ColorScaleRepeats=50.000000
         Opacity=0.600000
         FadeOutStartTime=4.300000
         FadeOut=True
         FadeInEndTime=1.000000
         FadeIn=True
         MaxParticles=1
         ForcedFade=True
         RespawnDeadParticles=False
         Name="MC"
         SpinParticles=True
         SpinCCWorCW=(X=0.000000)
         SpinsPerSecondRange=(X=(Min=0.040000,Max=0.040000))
         StartSizeRange=(X=(Min=0.287000,Max=0.287000),Y=(Min=0.287000,Max=0.287000),Z=(Min=0.287000,Max=0.287000))
         InitialParticlesPerSecond=10000.000000
         AutomaticInitialSpawning=False
         DrawStyle=PTDS_Brighten
         LifetimeRange=(Min=5.000000,Max=5.000000)
     End Object
     Emitters(0)=MeshEmitter'MeshEmitter4'
     Begin Object Class=MeshEmitter Name=MeshEmitter3
         StaticMesh=StaticMesh'LineageEffectsStaticmeshes.Support.supportenchant02'
         UseMeshBlendMode=False
         RenderTwoSided=True
         ColorScale(0)=(Color=(B=255,G=255,R=255,A=255))
         ColorScale(1)=(RelativeTime=1.000000,Color=(B=255,G=255,R=255,A=255))
         Opacity=0.310000
         FadeOutStartTime=0.280000
         FadeOut=True
         FadeInEndTime=0.110000
         FadeIn=True
         MaxParticles=20
         RespawnDeadParticles=False
         Name="RingsFlow"
         StartLocationOffset=(Z=5.000000)
         UseSizeScale=True
         UseRegularSizeScale=False
         SizeScale(0)=(RelativeTime=0.070000,RelativeSize=2.200000)
         SizeScale(1)=(RelativeTime=0.220000,RelativeSize=3.000000)
         SizeScale(2)=(RelativeTime=0.480000,RelativeSize=3.800000)
         SizeScale(3)=(RelativeTime=1.000000,RelativeSize=4.500000)
         StartSizeRange=(X=(Min=0.450000,Max=0.450000),Y=(Min=0.450000,Max=0.450000),Z=(Min=2.000000,Max=2.000000))
         InitialParticlesPerSecond=5.000000
         AutomaticInitialSpawning=False
         LifetimeRange=(Min=0.800000,Max=0.800000)
         StartVelocityRange=(Z=(Min=-40.000000,Max=-40.000000))
         VelocityLossRange=(Z=(Min=2.000000,Max=2.000000))
     End Object
     Emitters(1)=MeshEmitter'MeshEmitter3'
     bNoDelete=False
     DrawScale=0.250000
}
