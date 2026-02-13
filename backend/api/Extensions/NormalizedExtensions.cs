namespace api.Extensions;

public static class NormalizedExtensions
{
    public static string ToNormalized(this string text)
    {
        char[] chars = text.ToCharArray(); // convert strings to char for mutablity
        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = chars[i] switch
            {
                'ي' => 'ی',
                'ئ' => 'ی',
                'ك' => 'ک',
                // arabic to english (0660-0669)
                '٠' => '0',
                '١' => '1',
                '٢' => '2',
                '٣' => '3',
                '٤' => '4',
                '٥' => '5',
                '٦' => '6',
                '٧' => '7',
                '٨' => '8',
                '٩' => '9',
                // farsi to english (06F0-06F9)
                '۰' => '0',
                '۱' => '1',
                '۲' => '2',
                '۳' => '3',
                '۴' => '4',
                '۵' => '5',
                '۶' => '6',
                '۷' => '7',
                '۸' => '8',
                '۹' => '9',
                _ => chars[i]
            };
        }

        return new string(chars).Trim();
    }
}