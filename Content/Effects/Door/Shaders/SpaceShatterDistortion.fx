sampler uImage0 : register(s0); //需要加滤镜的纹理
sampler uImage1 : register(s1); //扭曲蒙版
sampler uImage2 : register(s2);//需要扭曲的区域
float R = 0.0;
float OtherStrength = 0.0;

//坐标旋转公式
float2 rotate(float2 Pos, float angle, float2 center)
{
    float s = sin(angle);
    float c = cos(angle);

    Pos -= center;

    float2 rotatedPos;
    rotatedPos.x = Pos.
    x * c - Pos.
    y * s;
    rotatedPos.y = Pos.
    x * s + Pos.
    y * c;

    rotatedPos += center;
    return rotatedPos;
}

float4 Main(float2 uv : TEXCOORD0) : COLOR0
{
    float u2 = tex2D(uImage2, uv).a;
    //强化扭曲效果
    u2 = clamp(u2 * 3.0, 0.0, 1.0);
    
    float4 col_0 = tex2D(uImage1, uv);
    float value = (col_0.y + col_0.z + col_0.a) / 4.0 * (0.05 + OtherStrength);
    //坐标偏移值
    float2 v = float2(value, 0.0);
    //将偏移坐标v扭转,纹理上这个像素越红，它扭曲的旋转角度越大
    float2 pos = rotate(uv + v, R + (col_0.x * 3.14159 * 2.0), uv);
    
    float4 col_1 = tex2D(uImage0, pos);
    
    return lerp(tex2D(uImage0, uv), col_1, u2);
}
technique Technique1
{
    pass Tentacle
    {
        PixelShader = compile ps_3_0 Main();
    }
}