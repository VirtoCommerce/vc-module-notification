
The email template coding - it's not easy, because each email service has its own characteristics - something is not supported or looks different from each other or on different devices. So here are some tips and tricks:

1. Try to declare styles for every individual element within its style attribute (for example <element style=”style:value;”></element>), otherwise known as 'inline CSS'. Если Вы будете использовать internal CSS (i.e. styles written within a <style> element) в шаблонах писем, то разных почтовых сервисов и с разных устройств Ваш шаблон может отобразиться по-разному, или где-то CSS стили не будут учитываться вообще.

![Correct template](/docs/media/template-correct-buttons.png)
<p align=center>The correct template with internal CSS</p>

![Template in Gmail](/docs/media/template-gmail-buttons.png)
<p align=center>The display template in Gmail service with internal CSS</p>

![Template in Outlook](/docs/media/template-outlook-buttons.png)
<p align=center>The display template in Outlook service with internal CSS</p>

2. Styles in Outlook service don't work within &lt;a&gt; tag. Use &lt;span&gt; or &lt;div&gt;.

3. Если необходимо добавить небольшие картинки, иконки в шаблон письма, то лучше всего использовать base64 format, т.к. в этом случае нет необходимости закидывать картинки в какой-либо внешний сервис и отображение картинок не зависит от работы этого сервиса. Но надо учитывать, что, например, сервис Gmail.com не всегда отображает картинки в формате base64.

4. Try to set widths in each cell rather than on the table. The combination of widths on the table, widths on the cells, HTML margins and padding, and CSS margins and padding can be chaotic.

5. If the spacing is critical to you, try nesting tables inside your main table instead. Even when margins and padding are supported by most email clients, results will be inconsistent.

![Template in iPhone - buttons are not correct](/docs/media/template-buttons-in-phone-not-correct.png)
<p align=center>The display template in iPhone emulator - IOS 15.0 in Safari browser without using table for buttons</p>

![Template in iPhone - buttons are correct](/docs/media/template-buttons-in-phone-correct.png)
<p align=center>The display template in iPhone emulator - IOS 15.0 in Safari browser with using table for buttons</p>
