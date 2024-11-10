Shader "DepthViewer"
{

    SubShader
    {
        Tags {  "RenderType" = "Transparent" "Queue" = "AlphaTest"   "IgnoreProjector" = "True" }  // 不透明オブジェクトとしてレンダリング
        ZWrite Off
		ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        //LOD 100
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld,v.vertex).xyz;
                o.screenPos= ComputeScreenPos(o.pos);
            
                return o;
            }
            
            //https://kurotorimkdocs.gitlab.io/kurotorimemo/030-Programming/Shader/CameraDepth2WorldPos/
            half4 frag (v2f i) : SV_Target
            {
                float4 screenPos = float4(i.screenPos.xyz,i.screenPos.w+0.00000000001);
                float4 screenPosNorm = screenPos/screenPos.w;//正規化されたスクリーン座標
                screenPosNorm.z = (UNITY_NEAR_CLIP_VALUE >= 0) ? screenPosNorm.z : screenPosNorm.z*0.5+0.5;
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,screenPosNorm.xy);
                return half4(depth,depth,depth,1);
            }
            
            ENDCG
        }
    }

}