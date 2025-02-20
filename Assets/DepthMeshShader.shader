Shader "Unlit/DepthMeshShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        //比例定数
        _k ("Value",float) = 1.0
        _threshold ("Threshold",float) = 0.05

    }
    SubShader
    {
        Tags { "Queue"="Transparent"  "RenderType"="Transparent"  }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

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
                float4 original : TEXCOORD1;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };
            //Depth y*width+xで取り出せる
            StructuredBuffer<float> _FloatBuffer;

            //テクスチャ
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _k;
            int _width;
            int _height;
            float _threshold;

            v2f vert (appdata v)
            {
                //y座標を反転して取り出し
                int index = (int) (_height-v.vertex.y-1)*_width+v.vertex.x;
                float depthValue = _FloatBuffer[index];
                
                float X = (v.vertex.x-_width/2.0)*depthValue*_k;
                float Y = (v.vertex.y-_height/2.0)*depthValue*_k;

                v2f o;
                o.original = v.vertex;
                //クリップ座標に変換
                o.vertex = UnityObjectToClipPos(float3(X,Y,depthValue));
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                //intに直すことで格子点で隣接座標との差を比べる
                int x = i.original.x;
                int y = _height-i.original.y-1;
                float p0 = _FloatBuffer[y*_width+x];
                float p1 = _FloatBuffer[y*_width+min(x+1,_width-1)];
                float p2 = _FloatBuffer[min(y+1,_height-1)*_width+x];
                float p3 = _FloatBuffer[min(y+1,_height-1)*_width+min(x+1,_width-1)];
                if (abs(p0-p1) > _threshold || abs(p0-p2)> _threshold || abs(p0-p3) > _threshold || abs(p1-p2) > _threshold || abs(p1-p3) > _threshold || abs(p2-p3) > _threshold) {
                    col *= (0,0,0,0);
                }
                
                return col;
            }
            ENDCG
        }
    }
}
