Shader "UI/Logo Shine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _ShineColor ("Shine Color", Color) = (1,1,1,1)
        _ShineWidth ("Shine Width", Range(0.01, 1)) = 0.15
        _ShineIntensity ("Shine Intensity", Range(0, 5)) = 1.5
        _ShineSpeed ("Shine Speed", Range(0, 5)) = 1
        _ShineAngle ("Shine Angle", Range(-90, 90)) = 45
        _ShineDelay ("Shine Delay", Range(0, 10)) = 2
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;

            float4 _Color;

            float4 _ShineColor;
            float _ShineWidth;
            float _ShineIntensity;
            float _ShineSpeed;
            float _ShineAngle;
            float _ShineDelay;

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);

                // --------------------------------
                // Shine direction
                // --------------------------------

                float angle = radians(_ShineAngle);

                float2 direction = float2(
                    cos(angle),
                    sin(angle)
                );

                // Center UV around 0
                float2 uv = i.uv - 0.5;

                // Position along shine direction
                float position = dot(uv, direction);

                // --------------------------------
                // Loop animation
                // --------------------------------

                float cycleLength = 2.0 + _ShineDelay;

                float time = fmod(
                    _Time.y * _ShineSpeed,
                    cycleLength
                );

                // Shine moves from left to right
                float shinePosition = -1.0 + time;

                // --------------------------------
                // Shine mask
                // --------------------------------

                float distance = abs(
                    position - shinePosition
                );

                float shine = 1.0 -
                    smoothstep(
                        0.0,
                        _ShineWidth,
                        distance
                    );

                // --------------------------------
                // Add shine
                // --------------------------------

                tex.rgb +=
                    _ShineColor.rgb *
                    shine *
                    _ShineIntensity;

                return tex * i.color;
            }

            ENDCG
        }
    }
}