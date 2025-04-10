Shader "Custom/VertexBillboardShader"
{
    Properties
    {
        _PointSize ("Point Size", Float) = 0.1
        _MainTex ("MainTex", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Pass
        {
            Cull Off
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.5   // Geometry Shaderを使うため 4.5 以上が必要

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2g
            {
                float2 uv :TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed2 quadPos : TEXCOORD1;
            };

            float _PointSize;
            sampler2D _MainTex;

            v2g vert(appdata v)
            {
                v2g o;
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            void generate_geom(v2g p,inout TriangleStream<g2f> triStream)
            {
                float3 camRight = UNITY_MATRIX_V[0].xyz;
                float3 camUp = UNITY_MATRIX_V[1].xyz;
                float halfSize = _PointSize * 0.5;
                float3 worldPos = p.worldPos;
                float3 p1 = worldPos + ( camRight + camUp) * halfSize;
                float3 p2 = worldPos + (-camRight + camUp) * halfSize;
                float3 p3 = worldPos + (-camRight - camUp) * halfSize;
                float3 p4 = worldPos + ( camRight - camUp) * halfSize;

                g2f o1, o2, o3, o4;

                float2 uv = p.uv;
                o1.uv = uv;
                o1.quadPos = float2(1,1);
                o1.vertex = UnityWorldToClipPos(p1);

                o2.uv = uv;
                o2.quadPos = float2(0,1);
                o2.vertex = UnityWorldToClipPos(p2);

                o3.uv = uv;
                o3.quadPos = float2(0,0);
                o3.vertex = UnityWorldToClipPos(p3);

                o4.uv = uv;
                o4.quadPos = float2(1,0);
                o4.vertex = UnityWorldToClipPos(p4);

                // 2つの三角形として四角形を描画
                triStream.Append(o1);
                triStream.Append(o2);
                triStream.Append(o3);
                triStream.RestartStrip();

                triStream.Append(o1);
                triStream.Append(o3);
                triStream.Append(o4);
                triStream.RestartStrip();
            }

            [maxvertexcount(18)]
            void geom(triangle v2g p[3], inout TriangleStream<g2f> triStream)
            {
                

                for (int i = 0;i < 3;i++)
                {
                    generate_geom(p[i],triStream);
                    triStream.RestartStrip();
                }
            }

            fixed4 frag(g2f i) : SV_Target
            {
                //-1~1に
                //二乗して1より大きい→半径1の円より大きい
                float2 quadPos = i.quadPos * 2.0 - 1.0;
                if (dot(quadPos, quadPos) > 1.0) discard;

                fixed4 col = tex2D(_MainTex,i.uv);
                return (1,1,1,1);
            }
            ENDCG
        }
    }
}