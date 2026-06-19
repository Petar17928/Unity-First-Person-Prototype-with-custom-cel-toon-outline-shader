void OutlineOffset_float(float3 vertexOS, float width, float maxWidth, out float3 result)
{
    float3 temp =  width * vertexOS * 10.0;
    result = temp;
}