Shader "Unlit/ProjectorShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {} // テクスチャを設定できるプロパティ
    }

    SubShader
    {
        Tags {  "RenderType" = "Transparent" "Queue" = "AlphaTest"   "IgnoreProjector" = "True" }  // 不透明オブジェクトとしてレンダリング
        
        ZWrite Off
		ZTest Always
        Cull Front
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

            sampler2D _MainTex;
            uniform float4x4 _PerspectiveMatrix;

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

                float eyeDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,screenPosNorm.xy));
                float3 cameraViewDir = -UNITY_MATRIX_V._m20_m21_m22;
                float3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
                float3 wpos = ((eyeDepth*worldViewDir * (1.0 / dot(cameraViewDir,worldViewDir)))+ _WorldSpaceCameraPos);
                
                float3 objectPos = mul(unity_WorldToObject, float4(wpos,1.0)).xyz;

                float3 frustumPos = mul(_PerspectiveMatrix,float4(objectPos,1.0)).xyz;
                frustumPos.xy /= objectPos.z;
                clip(abs(frustumPos.xyz+0.5));

                return tex2D(_MainTex,frustumPos.xy+0.5);
            }
            
            ENDCG
        }
    }

}