Shader "TownStudy/AniimoBackdrop" {
 Properties { _Tint("Tint",Color)=(1,1,1,1) }
 HLSLINCLUDE
 #pragma target 4.5
 #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
 #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
 #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
 struct Attributes {float3 positionOS:POSITION;float2 uv:TEXCOORD0;};
 struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
 Varyings Vert(Attributes i){Varyings o;o.positionCS=TransformObjectToHClip(i.positionOS);o.uv=i.uv;return o;}
 float4 Frag(Varyings i):SV_Target{
  // Adapted from the local AniimoStage five-stop gradient, without its floor or capture shadow dependencies.
  float t=saturate(i.uv.y);float3 color;
  float3 low=float3(.045,.13,.15),high=float3(.21,.29,.30);
  if(t<.45)color=lerp(low*.85,low,smoothstep(0,.45,t));else color=lerp(low,high,smoothstep(.45,1,t));
  color*=2.828425884;float radial=length((i.uv-float2(.48,.42))*float2(1.2,1));color*=lerp(1.05,.84,saturate(radial));
  return float4(color,1);
 }
 ENDHLSL
 SubShader{Tags{"RenderPipeline"="HDRenderPipeline" "RenderType"="Opaque"}
 Pass{Name "DepthForwardOnly" Tags{"LightMode"="DepthForwardOnly"} Cull Off ZWrite On ColorMask 0
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 ENDHLSL
 }
 Pass{Name "ForwardOnly" Tags{"LightMode"="ForwardOnly"} Cull Off ZWrite Off ZTest LEqual
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 ENDHLSL
 }
 }
}
