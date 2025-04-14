Shader "Unlit/DepthMeshShader"
{
    Properties
    {
        _BackgroundTexture ("BackgroundTexture", 2D) = "white" {}
        _BackgroundDepth ("BackgroundDepth", 2D) = "white" {}
        _ForeroundTexture ("ForegroundTexture", 2D) = "white" {}
        _ForegroundDepth ("ForegroundDepth", 2D) = "white" {}
        _threshold ("Threshold(m)",float) = 0.05
        _maxDistance ("MaxDistance",float) = 20
        _PointSize ("Point Size", float) = 0.1
        _width ("Width",int) = 640
        _height ("Height",int) = 480
        [MaterialToggle] _renderForeground ("RenderForeground",float) = 1
        [MaterialToggle] _renderBackground ("RenderBackground",float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent"  "RenderType"="Transparent"  }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            Cull Off
            ZWrite On
            Tags{ "LightMode" = "UniversalForward"}
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
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

            //テクスチャ
            sampler2D _ForegroundTexture;
            sampler2D _ForegroundDepth;
            float _maxDistance;
            float _PointSize;
            uniform float _renderForeground;

            v2g vert (appdata v)
            {
                v2g o;
                // ピクセル毎の色情報に乗せてきたデプス情報を復元する。[0.0, 1.0]
				float4 col = tex2Dlod(_ForegroundDepth, float4(v.uv.x, v.uv.y, 0, 0));
				// デプスカメラ座標系から空間に展開する。
                // 色は0~1に正規化されている
                
				v.vertex.x = v.vertex.x * col.x *_maxDistance;
				v.vertex.y = v.vertex.y * col.x *_maxDistance;
				v.vertex.z = col.x * _maxDistance;

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
                }
            }

            fixed4 frag (g2f i) : SV_Target
            {
                if (! _renderForeground) discard;
                //-1~1に
                //二乗して1より大きい→半径1の円より大きい
                float2 quadPos = i.quadPos * 2.0 - 1.0;
                if (dot(quadPos, quadPos) > 1.0) discard;
                return tex2D(_ForegroundTexture, i.uv);
            }
            ENDCG
        }
        
        Pass
        {
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            //テクスチャ
            sampler2D _BackgroundTexture;
            sampler2D _BackgroundDepth;
            float _threshold;
            float _maxDistance;
            int _width;
            int _height;
            uniform float _renderBackground;

            v2f vert (appdata v)
            {
                v2f o;
                // ピクセル毎の色情報に乗せてきたデプス情報を復元する。[0.0, 1.0]
				float4 col = tex2Dlod(_BackgroundDepth, float4(v.uv.x, v.uv.y, 0, 0));
				// デプスカメラ座標系から空間に展開する。
                // グレースケール色は0~1に正規化されている
				v.vertex.x = v.vertex.x * col.x *_maxDistance;
				v.vertex.y = v.vertex.y * col.x *_maxDistance;
				v.vertex.z = col.x * _maxDistance;
                
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                if (! _renderBackground) discard;
                fixed4 col = tex2D(_BackgroundTexture, i.uv);
                float x0 = floor(i.uv.x*_width)/_width;
                float x1 = (floor(i.uv.x*_width)+1)/_width;
                float y0 = floor(i.uv.y*_height)/_height;
                float y1 = (floor(i.uv.y*_height)+1)/_height;
                float4 d0 = tex2D(_BackgroundDepth, float2(x0,y0));				
                float4 d1 = tex2D(_BackgroundDepth, float2(x1,y0));				
                float4 d2 = tex2D(_BackgroundDepth, float2(x1,y1));
                float4 d3 = tex2D(_BackgroundDepth, float2(x0,y1));
                //tex2D is 0~maxDistance->0~1
                float threshold = _threshold/_maxDistance;
                // 深度の差が大きいところはアルファを0にして消す
                //_thresholdはm単位なのでmaxDistanceで割ることで0~1に正規化
                float d_min = min(min(d0.x, d1.x), min(d2.x, d3.x));
                float d_max = max(max(d0.x, d1.x), max(d2.x, d3.x));
                if (d_max - d_min > threshold) discard;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }

        
        
    }
}
