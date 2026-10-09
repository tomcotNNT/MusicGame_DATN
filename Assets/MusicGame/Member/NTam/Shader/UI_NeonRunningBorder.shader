Shader "Custom/UI_NeonRunningBorder"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {} // Thêm dòng này để fix lỗi thiếu _MainTex
        _Color ("Neon Color", Color) = (0, 1, 1, 1) // Màu neon (Mặc định Cyan)
        _BorderWidth ("Border Width", Range(0.005, 0.05)) = 0.015 // Độ dày viền
        _Speed ("Animation Speed", Float) = 4.0 // Tốc độ chạy của vệt sáng
        _GlowIntensity ("Glow Intensity", Float) = 3.0 // Độ phát sáng
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
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex; // Khai báo biến texture
            fixed4 _Color;
            float _BorderWidth;
            float _Speed;
            float _GlowIntensity;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                
                // Tính khoảng cách tới 4 cạnh để vẽ khung viền
                float2 edge = min(uv, 1.0 - uv);
                float distToEdge = min(edge.x, edge.y);

                // Nếu nằm ngoài độ dày viền thì bỏ qua (trong suốt)
                float isBorder = step(distToEdge, _BorderWidth);
                if (isBorder == 0.0) discard;

                // Tính toán tọa độ chạy vòng quanh chu vi hình chữ nhật
                float perimeterCoord = 0.0;
                if (uv.y <= _BorderWidth) perimeterCoord = uv.x; // Cạnh dưới
                else if (uv.x >= 1.0 - _BorderWidth) perimeterCoord = 1.0 + uv.y; // Cạnh phải
                else if (uv.y >= 1.0 - _BorderWidth) perimeterCoord = 3.0 + (1.0 - uv.x); // Cạnh trên
                else perimeterCoord = 4.0 + (1.0 - uv.y); // Cạnh trái

                // Tạo vệt sáng di chuyển theo thời gian (_Time.y)
                float beam = sin(perimeterCoord * 3.14159 * 2.0 - _Time.y * _Speed) * 0.5 + 0.5;
                beam = pow(beam, 4.0); // Làm vệt sáng sắc nét và rực rỡ hơn

                // Tổng hợp màu sắc và độ sáng phát quang (Glow)
                float3 finalColor = IN.color.rgb * (1.0 + beam * _GlowIntensity);
                float alpha = IN.color.a * (0.4 + beam * 0.6); // Viền luôn sáng mờ và bùng sáng khi tia sáng chạy qua

                return fixed4(finalColor, alpha);
            }
            ENDCG
        }
    }
}