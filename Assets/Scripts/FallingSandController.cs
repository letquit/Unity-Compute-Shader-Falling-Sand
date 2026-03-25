using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 落沙效果控制器，使用Compute Shader实现粒子物理模拟
/// </summary>
public class FallingSandController : MonoBehaviour
{
    /// <summary>
    /// 鼠标作用范围半径
    /// </summary>
    public float range = 10f;
    
    /// <summary>
    /// 落沙效果的Compute Shader
    /// </summary>
    public ComputeShader fallingSandShader;
    
    /// <summary>
    /// 存储落沙效果的渲染纹理
    /// </summary>
    public RenderTexture fallingSandRT;
    
    /// <summary>
    /// UI图像组件引用
    /// </summary>
    private Image imageUI;

    /// <summary>
    /// 当前鼠标位置坐标
    /// </summary>
    public Vector2Int position;
    
    /// <summary>
    /// 沙粒下落方向控制标志（左右交替）
    /// </summary>
    private bool rightFirst = true;
    
    /// <summary>
    /// 是否正在生成沙子的标志
    /// </summary>
    private bool spawnSand = false;
    
    /// <summary>
    /// 当前绘制颜色
    /// </summary>
    private Color color;

    /// <summary>
    /// 红色通道变化偏移量
    /// </summary>
    private float offset_r = .001f;
    
    /// <summary>
    /// 绿色通道变化偏移量
    /// </summary>
    private float offset_g = .002f;
    
    /// <summary>
    /// 蓝色通道变化偏移量
    /// </summary>
    private float offset_b = .003f;
    
    // Kernels
    /// <summary>
    /// 初始化内核名称
    /// </summary>
    private string initializeKernel = "Initialize";
    
    /// <summary>
    /// 沙粒下落计算内核名称
    /// </summary>
    private string fallingSandKernel = "SandFall";
    
    // Property
    /// <summary>
    /// 沙粒纹理属性名
    /// </summary>
    private string sandRTProperty = "sandRT";
    
    /// <summary>
    /// X轴位置属性名
    /// </summary>
    private string posXProperty = "posX";
    
    /// <summary>
    /// Y轴位置属性名
    /// </summary>
    private string posYProperty = "posY";
    
    /// <summary>
    /// 作用范围属性名
    /// </summary>
    private string rangeProperty = "range";
    
    /// <summary>
    /// 右侧优先标志属性名
    /// </summary>
    private string rightFirstProperty = "rightFirst";
    
    /// <summary>
    /// 生成沙子标志属性名
    /// </summary>
    private string colorProperty = "color";
    
    /// <summary>
    /// 颜色向量属性名
    /// </summary>
    private string spawnSandProperty = "spawnSand";

    /// <summary>
    /// 初始化方法，在对象被创建时调用
    /// </summary>
    private void Awake()
    {
        // Create Render Texture
        CreateRenderTexture();
        // Set Material to Image UI
        SetMaterialTexture();
        // Initialize Render Texture
        Dispatch(initializeKernel);
    }

    /// <summary>
    /// 每帧更新输入状态
    /// 处理鼠标点击和滚轮事件
    /// </summary>
    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame) spawnSand = true;
        else if (Mouse.current.leftButton.wasReleasedThisFrame) spawnSand = false;

        float scrollValue = Mouse.current.scroll.ReadValue().y;
        if (scrollValue > 0)
        {
            range++;
        }
        else if (scrollValue < 0)
        {
            range--;
        }
    }

    /// <summary>
    /// 固定时间步长更新物理计算
    /// 更新鼠标位置、执行沙粒物理计算并调整颜色
    /// </summary>
    private void FixedUpdate()
    {
        position = new Vector2Int((int)Mouse.current.position.ReadValue().x, (int)Mouse.current.position.ReadValue().y);

        // 执行三次沙粒下落计算以获得更流畅的效果
        for (int i = 0; i < 3; i++)
        {
            Dispatch(fallingSandKernel);
            rightFirst = !rightFirst;
        }
        
        // 平滑更新颜色值
        color.r += offset_r;
        color.g += offset_g;
        color.b += offset_b;

        // 处理红色通道边界反弹
        if (color.r >= 1f) offset_r *= -1f;
        if (color.g >= 1f) offset_g *= -1f;
        if (color.b >= 1f) offset_b *= -1f;
        
        // 处理颜色通道下边界反弹
        if (color.r <= 0f) offset_r *= -1f;
        if (color.g <= 0f) offset_g *= -1f;
        if (color.b <= 0f) offset_b *= -1f;
    }

    /// <summary>
    /// 创建用于存储沙粒效果的渲染纹理
    /// </summary>
    private void CreateRenderTexture()
    {
        fallingSandRT = new RenderTexture(Screen.width, Screen.height, 24);
        fallingSandRT.enableRandomWrite = true;
        fallingSandRT.filterMode = FilterMode.Point;
        fallingSandRT.Create();
    }

    /// <summary>
    /// 设置UI材质的纹理
    /// 将渲染纹理应用到Image组件上显示
    /// </summary>
    private void SetMaterialTexture()
    {
        imageUI = GetComponent<Image>();
        imageUI.material.SetTexture("_MainTex", fallingSandRT);
    }

    /// <summary>
    /// 分发Compute Shader内核执行计算
    /// </summary>
    /// <param name="kernel">要执行的内核名称</param>
    private void Dispatch(string kernel)
    {
        int kernel_handle = fallingSandShader.FindKernel(kernel);
        fallingSandShader.SetTexture(kernel_handle, sandRTProperty, fallingSandRT);
        fallingSandShader.SetFloat(posXProperty, position.x);
        fallingSandShader.SetFloat(posYProperty, position.y);
        fallingSandShader.SetFloat(rangeProperty, range);
        fallingSandShader.SetBool(rightFirstProperty, rightFirst);
        fallingSandShader.SetBool(spawnSandProperty, spawnSand);
        fallingSandShader.SetVector(colorProperty, color);
        fallingSandShader.Dispatch(kernel_handle, (fallingSandRT.width / 8), (fallingSandRT.height / 8), 1);
    }
}
