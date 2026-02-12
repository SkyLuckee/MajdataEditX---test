using System.Windows;

namespace MajdataEdit;

// 查找与替换

public partial class MainWindow : Window
{
    private bool isReplaceConformed;

    public void FindAndScroll()
    {
        isFinding = true;

        string content = FumenContent.Text; //这里完全依赖UI元素，不管Raw是什么了
        string keyword = InputText.Text;

        // 为空
        if (string.IsNullOrEmpty(keyword)) return;
        // 防止 findPosition 越界（比如文本被删除变短了）
        if (findPosition >= content.Length) findPosition = 0;
        // 下一个
        int position = content.IndexOf(keyword, findPosition);
        // 没有下一个了
        if (position == -1 && findPosition > 0) position = content.IndexOf(keyword, 0);
        //彻底没找到
        if (position == -1)
        {
            findPosition = 0;
            return;
        }

        needChangeTime = true;
        FumenContent.Select(position, keyword.Length);
        lastFindPosition = position;
        findPosition = position + keyword.Length;
        FumenContent.Focus();
    }

    public void FindAndReplace()
    {
        if (FumenContent.SelectionStart == lastFindPosition)
            FumenContent.SelectedText = ReplaceText.Text;

        FindAndScroll();
    }
}