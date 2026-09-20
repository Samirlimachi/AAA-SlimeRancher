Shader "Area1/Colores VR"
{
 Properties { _BaseColor("Tint",Color)=(1,1,1,1) _Smoothness("Smoothness",Range(0,1))=.3 }
 SubShader
 {
  Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
  Pass
  {
   Name "ForwardLit"
   Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor;
    half _Smoothness;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; half4 color:COLOR; half fog:TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
   Varyings Vert(Attributes i) {
    Varyings o=(Varyings)0;UNITY_SETUP_INSTANCE_ID(i);UNITY_TRANSFER_INSTANCE_ID(i,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    VertexPositionInputs p=GetVertexPositionInputs(i.positionOS.xyz);o.positionCS=p.positionCS;o.positionWS=p.positionWS;
    o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.color=i.color*_BaseColor;o.fog=ComputeFogFactor(p.positionCS.z);return o;
   }
   half4 Frag(Varyings i):SV_Target {
    UNITY_SETUP_INSTANCE_ID(i);UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    half3 n=normalize(i.normalWS);Light light=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
    half diffuse=saturate(dot(n,light.direction));half3 ambient=max(SampleSH(n),half3(.16,.16,.18));
    half3 col=i.color.rgb*(ambient+light.color*(.12+.88*diffuse)*light.shadowAttenuation);
    half3 h=normalize(light.direction+GetWorldSpaceNormalizeViewDir(i.positionWS));
    col+=light.color*pow(saturate(dot(n,h)),lerp(12,100,_Smoothness))*_Smoothness*.35*light.shadowAttenuation;
    return half4(MixFog(col,i.fog),1);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
  UsePass "Universal Render Pipeline/Lit/DepthNormals"
 }
}
