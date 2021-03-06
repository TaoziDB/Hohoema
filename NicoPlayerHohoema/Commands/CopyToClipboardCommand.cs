﻿using Prism.Commands;

namespace NicoPlayerHohoema.Commands
{
    public sealed class CopyToClipboardCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is Interfaces.INiconicoContent)
            {
                var content = parameter as Interfaces.INiconicoContent;

                var shareContent = Helpers.ShareHelper.MakeShareText(content);
                Helpers.ShareHelper.CopyToClipboard(shareContent);
            }
            else if (parameter != null)
            {
                Helpers.ShareHelper.CopyToClipboard(parameter.ToString());
            }
        }
    }
}
