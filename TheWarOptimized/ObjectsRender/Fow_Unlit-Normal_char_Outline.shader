// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Unlit shader. Simplest possible textured shader.
// - no lighting
// - no lightmap support
// - no per-material color

Shader "FowCustom/Unlit/Texturesl_OutlineStencil" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_reflect_df("reflect_df", CUBE) = "white" {}
		_speed ("speed", Range(0, 10)) = 1
		_max ("max", Range(0.1, 1)) = 1
		_r ("light", Range(0.1, 3)) = 1
		_b ("base", Range(0.1, 1)) = 1
		_TeamColor ("TeamColor", Color) = (0,0,0,1)
		_FOWColor("Color", Color) = (0.5, 0.5, 0.5, 1)
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 100
		UsePass"CustomPass/Passes/SHADOWCASTER"
		UsePass"CustomPass/Passes/FORWARDBASESHADOWOPTIMIZEDOUTLINEPRE"
		UsePass"CustomPass/Passes/OUTLINE"
	}
}
