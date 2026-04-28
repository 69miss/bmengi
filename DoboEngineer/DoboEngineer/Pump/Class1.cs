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

        public string TopLiquidColor { get => _topLiquidColor; set => SetField(ref _topLiquidColor, value); }
        public string BottomLiquidColor { get => _bottomLiquidColor; set => SetField(ref _bottomLiquidColor, value); }

        // --- 上方罐体 ---
        public double TopLevel
        {
            get => _topLevel;
            set
            {
                if (SetField(ref _topLevel, value))
                {
                    OnPropertyChanged(nameof(TopLevelPercentage));
                    OnPropertyChanged(nameof(TopLiquidPixelHeight));
                    OnPropertyChanged(nameof(IsTopLiquidVisible)); // 触发可见性更新
                    OnPropertyChanged(nameof(TopTankText));
                }
            }
        }
        public double TopLevelPercentage => Math.Clamp(TopLevel / 100.0, 0.0, 1.0);
        // ⭐ 修改：基于罐体中间长方形的高度 (93.36像素) 进行换算
        public double TopLiquidPixelHeight => TopLevelPercentage * 93.36;
        public bool IsTopLiquidVisible => TopLevel > 0; // 大于0时才显示液体和波浪
        public string TopTankText => $"罐体1\n{TopLevel:F0} L";


        // --- 下方罐体 ---
        public double BottomLevel
        {
            get => _bottomLevel;
            set
            {
                if (SetField(ref _bottomLevel, value))
                {
                    OnPropertyChanged(nameof(BottomLevelPercentage));
                    OnPropertyChanged(nameof(BottomLiquidPixelHeight));
                    OnPropertyChanged(nameof(IsBottomLiquidVisible));
                }
            }
        }
        public double BottomLevelPercentage => Math.Clamp(BottomLevel / 100.0, 0.0, 1.0);
        // ⭐ 修改：同理
        public double BottomLiquidPixelHeight => BottomLevelPercentage * 93.36;
        public bool IsBottomLiquidVisible => BottomLevel > 0;
        public string BottomTankText => "罐体2";


        // --- 阀门 ---
        public bool IsValveOpen
        {
            get => _isValveOpen;
            set { if (SetField(ref _isValveOpen, value)) OnPropertyChanged(nameof(ValveText)); }
        }
        public string ValveText => $"阀门 {(IsValveOpen ? "开" : "关")}";


        // --- 基础代码 ---
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