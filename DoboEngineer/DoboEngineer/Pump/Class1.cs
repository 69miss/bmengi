using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DoboEngineer
{
    public class TankPairViewModel : INotifyPropertyChanged
    {
        private double _topLevel;
        private double _bottomLevel;
        private bool _isValveOpen;
        private string _topLiquidColor = "#4A90E2";
        private string _bottomLiquidColor = "#48BB78";
        private string _topTankText = "罐体1\n0 L";
        private string _bottomTankText = "罐体2";
        private string _valveText = "阀门 关";

        // ================= 外部可绑定的自由属性 =================
        public string TopLiquidColor { get => _topLiquidColor; set => SetField(ref _topLiquidColor, value); }
        public string BottomLiquidColor { get => _bottomLiquidColor; set => SetField(ref _bottomLiquidColor, value); }

        // 自定义罐体文字
        public string TopTankText { get => _topTankText; set => SetField(ref _topTankText, value); }
        public string BottomTankText { get => _bottomTankText; set => SetField(ref _bottomTankText, value); }

        // 自定义阀门文字
        public string ValveText { get => _valveText; set => SetField(ref _valveText, value); }

        // 阀门开闭状态 (控制管道水流动画)
        public bool IsValveOpen { get => _isValveOpen; set => SetField(ref _isValveOpen, value); }

        // 上方液位高度 (0-100)
        public double TopLevel
        {
            get => _topLevel;
            set
            {
                if (SetField(ref _topLevel, value))
                {
                    OnPropertyChanged(nameof(TopLiquidPixelHeight));
                    OnPropertyChanged(nameof(IsTopLiquidVisible));
                }
            }
        }

        // 下方液位高度 (0-100)
        public double BottomLevel
        {
            get => _bottomLevel;
            set
            {
                if (SetField(ref _bottomLevel, value))
                {
                    OnPropertyChanged(nameof(BottomLiquidPixelHeight));
                    OnPropertyChanged(nameof(IsBottomLiquidVisible));
                }
            }
        }

        // ================= 内部计算属性 (供 UI 自动换算使用) =================
        // 93.36 是罐体直筒段的精确像素高度
        public double TopLiquidPixelHeight => Math.Clamp(TopLevel / 100.0, 0.0, 1.0) * 93.36;
        public bool IsTopLiquidVisible => TopLevel > 0;

        public double BottomLiquidPixelHeight => Math.Clamp(BottomLevel / 100.0, 0.0, 1.0) * 93.36;
        public bool IsBottomLiquidVisible => BottomLevel > 0;

        // INotifyPropertyChanged 基础实现
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}