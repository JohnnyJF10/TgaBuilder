﻿using System.Windows.Input;
using TgaBuilderLib.Abstraction;
using TgaBuilderLib.Commands;

namespace TgaBuilderLib.ViewModel.Views
{
    public class AboutViewModel : ViewModelBase
    {
        private RelayCommand<IView>? _closeCommand;
        public ICommand CloseCommand => _closeCommand ??= new RelayCommand<IView>(v => v.Close());

        public string Title
            => $"TgaBuilder - {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "Unknown Version"}";

        public string Subtitle
            => "Texture Sets Building Tool";

        public string DotNetInfo
            => $".Net {System.Environment.Version.Major}.{System.Environment.Version.Minor} Version tool";

        public string AboutText 
            => "TgaBuilder is a Texture Sets Building Tool which is intended to facilitate the process of " +
            "texture panel creation used for TRLE. " +
            "\r\n" +
            "\r\n" +
            "The tool is inspired by TBuilder by IceBerg but " +
            "programmed from scratch in .net 8, C# WPF by Jonson. " +
            "\r\n" +
            "\r\n" +
            "The tool and the source code are licensed under MIT. " +
            "\r\n" +
            "\r\n" +
            "TgaBuilder is an independent software, not supported by Crystal Dynamics or the Embracer Group.";
    }
}
