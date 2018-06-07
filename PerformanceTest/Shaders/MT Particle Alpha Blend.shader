// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "MT/Particles/Alpha Blended" {
Properties {
	_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
	_MainTex ("Particle Texture", 2D) = "white" {}
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
	Blend SrcAlpha OneMinusSrcAlpha
	ColorMask RGB
	Cull Off Lighting Off ZWrite Off

	SubShader {
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragParticle
			//#pragma target 2.0
			#pragma multi_compile_particles
			//#pragma multi_compile_fog
			#include "UnityCG.cginc"
			#include "MT.cginc"
			ENDCG
		}
	}	
}
}
