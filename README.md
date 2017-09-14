# KxLib
自用C#/WPF类库
包含3个部分
- KxLib 工具类
- KxLib.Database 数据库操作(SQLite)
- KxLib.UI WPF界面类库

## KxLib 工具类
- FileHelper 文件操作助手
- HttpHelper 网络通讯助手
- Logger 日志
- StringConverter 字符串转换
- StringHelper 字符串助手
- StringUtils 字符串相关
- TimeHelper 时间助手
- LitJson 知名项目嵌入进来了

## KxLib.Database 数据库操作(SQLite)
很不完善 不推荐用

## KxLib.UI WPF界面类库
- KxWindow 主界面
- KxSmoothProgressBase 平滑的进度条

### KxWindow 用法
App.xaml 加入这条
```
    <Application.Resources>
        <ResourceDictionary> 
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="pack://application:,,,/KxLib.UI;Component/Themes/AllThemes.xaml" ></ResourceDictionary>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
```
并且替换WPF窗体的头部为这个 就可以使用了
```
<kxui:KxWindow x:Class="程序集.窗体名"
        xmlns:kxui="clr-namespace:KxLib.UI;assembly=KxLib.UI"
        mc:Ignorable="d"
        Title="窗体标题" Height="408" Width="615" MinHeight="400" MinWidth="600"
             SkinType="Light" IsShowMax="True" WindowStartupLocation="CenterScreen" 
             IsShowSkin="True" Margin="0">
```