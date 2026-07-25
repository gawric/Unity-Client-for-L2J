class el_ice_bolt_ta extends Emitter;

defaultproperties
{
     
    
     Begin Object Class=SpriteEmitter Name=SpriteEmitter1
         ColorScale(0)=(Color=(B=255,G=255,R=255,A=255))
         ColorScale(1)=(RelativeTime=1.000000,Color=(B=255,G=255,R=255,A=255))
         Opacity=0.200000
         FadeOutStartTime=1.000000
         FadeInEndTime=0.150000
         FadeIn=True
         MaxParticles=12
         RespawnDeadParticles=False
         Name="Steam" //Облачка
         StartLocationOffset=(Z=1.000000)
         StartLocationRange=(X=(Min=-25.000000,Max=25.000000),Y=(Min=-25.000000,Max=25.000000))
         StartLocationShape=PTLS_Polar
         StartLocationPolarRange=(X=(Max=360.000000),Y=(Min=85.000000,Max=95.000000),Z=(Min=16.000000,Max=16.000000))
         SpinParticles=True
         SpinsPerSecondRange=(X=(Min=0.100000,Max=0.100000),Y=(Min=0.050000,Max=0.100000),Z=(Min=0.050000,Max=0.100000))
         StartSpinRange=(X=(Max=1.000000))
         UseSizeScale=True
         UseRegularSizeScale=False
         UniformSize=True
         SizeScale(0)=(RelativeTime=1.000000,RelativeSize=1.200000)
         StartSizeRange=(X=(Min=12.000000,Max=12.000000),Y=(Min=12.000000,Max=12.000000),Z=(Min=12.000000,Max=12.000000))
         InitialParticlesPerSecond=4.000000
         AutomaticInitialSpawning=False
         DrawStyle=PTDS_AlphaBlend
         Texture=Texture'LineageEffectsTextures.Particles.fx_m_t0035'
         TextureUSubdivisions=8
         TextureVSubdivisions=8
         BlendBetweenSubdivisions=True
         SubdivisionStart=4
         SubdivisionEnd=15
         LifetimeRange=(Min=1.000000,Max=1.000000)
         InitialDelayRange=(Min=0.200000,Max=0.200000)
         StartVelocityRange=(X=(Min=10.000000,Max=10.000000),Y=(Min=10.000000,Max=10.000000),Z=(Min=-40.000000,Max=-20.000000))
         GetVelocityDirectionFrom=PTVD_StartPositionAndOwner
     End Object
    
     Emitters(4)=MeshEmitter'MeshEmitter7'
     bNoDelete=False
     DrawScale=0.050000
     bDirectional=True
}
