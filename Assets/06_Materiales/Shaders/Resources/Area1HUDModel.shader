// 3D icons on the head-locked HUD (heart, coin): textured, simple lighting, always drawn on top
// of the world like the HUD UI. Lives in Resources so Shader.Find works in builds.
Shader "Area1/HUD Model" {
 Properties { _MainTex("Texture",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _Alpha("Alpha",Range(0,1))=1 }
 SubShader {
 Tags {"Queue"="Overlay+1" "RenderType"="Transparent" "IgnoreProjector"="True"}
 Cull Back Lighting Off ZWrite On ZTest Always Blend SrcAlpha OneMinusSrcAlpha
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float light:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
 sampler2D _MainTex;float4 _MainTex_ST;fixed4 _Color;float _Alpha;
 v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.vertex=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);
  float3 n=normalize(mul((float3x3)UNITY_MATRIX_IT_MV,v.normal));o.light=.55+.45*saturate(dot(n,normalize(float3(-.4,.6,.7))));return o;}
 fixed4 frag(v2f i):SV_Target {fixed4 c=tex2D(_MainTex,i.uv)*_Color;c.rgb*=i.light;c.a*=_Alpha;return c;}
 ENDCG
 }
 }
}
