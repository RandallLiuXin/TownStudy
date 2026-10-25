Shader "TownStudy/CapturedHouse" {
 Properties {
  _MainTex("Captured House Data",2D)="white"{} _TypeTex("Captured Type Data",2D)="black"{} _Palette("Captured Palette",2D)="white"{}
  _PaletteIndex("Palette Index",Float)=0 _RoofColor("Roof Color",Vector)=(.8584906,.5565478,.376602,1)
  _SunColor("Captured Sun",Vector)=(.708038,.617508,.549336,1) _AmbientColor("Captured Ambient",Vector)=(.492815,.586108,.686668,1)
  _LightDirection("Light Toward Surface",Vector)=(-.00858,.71724,-.69677,0)
  _Occlusion("Volume Light Approximation",Range(.1,1))=.7 _EdgeStrength("Edge Strength",Range(0,1))=.6
  _Raw("Raw Prop Atlas",Float)=0 _CornerMask("Occupied Corners",Float)=255
 }
 HLSLINCLUDE
 #pragma target 4.5
 #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
 #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
 #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
 #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);TEXTURE2D(_TypeTex);SAMPLER(sampler_TypeTex);TEXTURE2D(_Palette);SAMPLER(sampler_Palette);
 float4 _RoofColor,_SunColor,_AmbientColor,_LightDirection;
 float _PaletteIndex,_Occlusion,_EdgeStrength,_Raw,_CornerMask;
 struct Attributes{float3 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;float2 edge:TEXCOORD1;};
 struct Varyings{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 normalWS:TEXCOORD1;float2 edge:TEXCOORD2;};
 Varyings Vert(Attributes i){Varyings o;o.positionCS=TransformObjectToHClip(i.positionOS);o.normalWS=TransformObjectToWorldNormal(i.normalOS);float paletteOffset=(((uint)_CornerMask&(1u<<(uint)i.edge.x))!=0)?0:18;o.uv=float2(i.uv.x+(_Raw<.5&&_PaletteIndex==0?paletteOffset:0),frac(i.uv.y*.5)*2);uint flags=(uint)(floor(i.uv.y+.5)*.5);o.edge=float2((int)((flags>>1)&1)-(int)(flags&1),(int)((flags>>3)&1)-(int)((flags>>2)&1));return o;}
 float4 Frag(Varyings i):SV_Target{
  // EID261 PS31-32: negative-U prop faces are stencil helpers, not visible surfaces.
  if(_Raw>.5)clip(i.uv.x);
  // EID185 PS31-47: alpha controls two successive blends, not opacity.
  float2 texel=i.uv*128;float2 footprint=max(sqrt(ddx(texel)*ddx(texel)+ddy(texel)*ddy(texel)),.00001);
  float2 aa=saturate(abs(frac((texel+1.5)*.5)*2-1)/footprint);
  float2 sampleUv=lerp(floor((texel+1.5)*.5)*2-.5,floor((texel+.5)*.5)*2+.5,aa)/128;
  float4 source=SAMPLE_TEXTURE2D_LOD(_MainTex,sampler_MainTex,sampleUv,0);
  float4 type=SAMPLE_TEXTURE2D_LOD(_TypeTex,sampler_TypeTex,sampleUv,0);
  float index=floor(abs(i.uv.x))+_PaletteIndex;
  float3 palette=SAMPLE_TEXTURE2D_LOD(_Palette,sampler_Palette,float2((index+.5)/32,.25),0).rgb;
  float3 tint=saturate(lerp(palette,_RoofColor.rgb,type.g*1.4));
  float gray=dot(source.rgb,float3(.2,.7,.1));
  float3 color=lerp(lerp(tint,max(0,tint+gray-.5),source.a),source.rgb,source.a);
  color=lerp(color,source.rgb,_Raw);
  float2 edge=saturate((1-abs(i.edge))/max(fwidth(i.edge),.0001));
  color*=lerp(1,.5+.5*min(edge.x,edge.y),_EdgeStrength*(1-_Raw));
  // EID185 PS107-122. The captured volume/shadow maps are replaced with editable normal lighting.
  float n=saturate(dot(normalize(i.normalWS),normalize(_LightDirection.xyz)));
  float sun=1-pow(1-n,3);
  color*=_Occlusion*lerp(1.8*_AmbientColor.rgb,1.4*_SunColor.rgb,sun)+.4*sun*_SunColor.rgb+.1;
  float luma=dot(color,float3(.2,.7,.1));color+=(2*luma-.6)*.1;
  color+=(.6-color)*float3(-.2,-.05,.2);
  // Original UNORM color path was gamma-valued; HDRP output is linear.
  return float4(SRGBToLinear(saturate(color)),1);
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
