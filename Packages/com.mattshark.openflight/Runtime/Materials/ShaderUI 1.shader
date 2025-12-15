Shader "Custom/ShaderUI_Vert"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture State Off", 2D) = "white" {}
        [NoScaleOffset] _MainTexOff ("Texture State On", 2D) = "white" {}
        _Cutoff("AlphaCutout", Range(0.0, 1.0)) = 0.2
        [Space(30)]
        _Scale ("Scale", Range(0.0, 1.0)) = 0.01
        [IntRange] _Rotation("Rotation", Range(0, 360)) = 180
        _XTranslate ("X axis", Range(-1, 1)) = 0.0
        _YTranslate ("Y axis", Range(-1, 1)) = 0.0
        [Space(30)]
        [MaterialToggle] _SwapState ("Swap icon state", int) = 0
 


    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Overlay" }
        ZTest Always
        ZWrite Off
        Cull Off

        Pass {
            CGPROGRAM
            
            // Use shader model 3.0 target, to get nicer looking lighting
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_compile_prepassfinal noshadowmask nodynlightmap nodirlightmap nolightmap

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };


            float4 _Color;
            sampler2D _MainTex;
            sampler2D _MainTexOff;
            float4 _MainTex_ST;

            uint _Rotation;
            float _Scale;
            float _XTranslate;
            float _YTranslate;
            float _Cutoff;
            int _SwapState;

            //Vertex

            v2f Vertex(appdata i) {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                //o.vertex = mul(unity_ObjectToWorld, i.vertex);
                o.vertex = i.vertex;
                float xRatio = _ScreenParams.y/_ScreenParams.x;
                float radianRotation = radians(_Rotation);

                //Rotate Quad
                float2 currentXY = o.vertex.xy;
                o.vertex.x = (currentXY.x * cos(radianRotation)) - (currentXY.y * sin(radianRotation)); 
                o.vertex.y = (currentXY.y * cos(radianRotation)) + (currentXY.x * sin(radianRotation));

                //Scale
                o.vertex.xy *= _Scale;

                //Correct for screen aspect ratio
                o.vertex.x *= xRatio;

                //Transform
                o.vertex.x += _XTranslate;
                o.vertex.y += _YTranslate;


                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                o.uv *= _MainTex_ST;
                return o;
            }

            //
            // https://github.com/cnlohr/shadertrixx
            //

            bool isVR()
            {
                #if defined(USING_STEREO_MATRICES)
                    return true;
                #else
                    return false;
                #endif
            }

            bool isDesktop() { return !isVR() && abs(UNITY_MATRIX_V[0].y) < 0.0000005; }

            bool isRightEye()
            {
                #if defined(USING_STEREO_MATRICES)
                    return unity_StereoEyeIndex == 1;
                #else
                    return false;
                #endif
            }

            //
            //
            //

            float4 Fragment(v2f input) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); 
                float4 col = (0,0,0,0);

                if (isRightEye() || isDesktop())
                {
                    if (_SwapState == 0) {
                        col = tex2D(_MainTex, input.uv);
                    }
                    else {
                        col = tex2D(_MainTexOff, input.uv);
                    }

                    if (col.a < _Cutoff)
                        discard;
                }
                return col * _Color;
            }

            ENDCG
        }
    }
}
