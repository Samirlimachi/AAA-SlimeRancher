// Particles for AREA1 effects (plort stars, water). Vertex color x texture, blend chosen per material
// (_SrcBlend/_DstBlend: One/One = glowing additive, SrcAlpha/OneMinusSrcAlpha = water).
// Lives in Resources so Shader.Find works in builds.
Shader "Area1/Particle" {
 Properties { _MainTex("Texture",2D)="white"{} [HideInInspector]_SrcBlend("Src",Float)=5 [HideInInspector]_DstBlend("Dst",Float)=10 [HideInInspector]_Additive("Additive",Float)=0 }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
 Cull Off Lighting Off ZWrite Off Blend [_SrcBlend] [_DstBlend]
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct v2f {float4 vertex:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
 sampler2D _MainTex;float _Additive;
 v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv;return o;}
 fixed4 frag(v2f i):SV_Target {fixed4 c=tex2D(_MainTex,i.uv)*i.color;c.rgb*=lerp(1,c.a,_Additive);return c;}
 ENDCG
 }
 }
}
