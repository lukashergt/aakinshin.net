---
title: "Получение MCP: личный опыт"
date: "2014-07-24"
tags:
- Exams
- dotnet
- cs
- Microsoft
- MCP
aliases:
- /ru/blog/dotnet/ms-mcp/
- /ru/blog/post/ms-mcp/
---

Не так давно мне на работе перепал ваучер на бесплатную сдачу экзамена от Microsoft. «А почему бы и нет?» — подумал я. План был выполнен успешно, в связи с чем мне хотелось бы поделиться личным опытом. Возможно, эта история пригодится тем, кто только собирается начать получать сертификации от Microsoft.

<p class="center">
  <img src="/img/posts/dotnet/ms-mcp/mvp-roadmap.png" />
</p>

<!--more-->

### Выбираем экзамен

Я очень долго медитировал на схему, приведённую выше. Ух, ну и понаделали же разных экзаменов! Увы, большинство из них (всякие Lync Server, SharePoint, JavaScript и т.п.) не привлекли моего внимания. Выбор остановился на экзамене [070-483](https://www.microsoft.com/learning/ru-ru/exam-70-483.aspx) «Programming in C#».

### Готовимся к экзамену

Перед экзаменом полагается готовиться.
Я пришёл к выводу, что лучшей подготовкой к экзамену будет несколько лет программирования на C#.
Если хочется почитать книжку, то лучше всего пойдёт классика под .NET: [CLR via C#](https://www.goodreads.com/book/show/16033480-clr-via-c)	от Джеффри Рихтера.
Но, давайте взглянем, что же предлагает нам интернет в плане подготовки.
Первой книжкой, которая попалась мне на глаза, была	[MCSD Certification Toolkit (Exam 70-483): Programming in C#](https://www.goodreads.com/book/show/17082095-mcsd-certification-toolkit-exam-70-483).
Ох, лучше бы я её никогда не открывал. И вам не советую. С первых же глав в глаза бросается чудовищное количество ошибок. Причём, не спорных мест, а именно очень грубых ошибок. Готовиться по ней к чему бы то ни было просто не представляется возможным.
Второй книжка была [Exam Ref 70-483: Programming in C#](https://www.goodreads.com/book/show/16144492-exam-ref-70-483) от замечательного человека по имени [Wouter de Kort](http://wouterdekort.blogspot.ru/). Это уже намного более адекватное чтиво. Не скажу, что по ней можно на 100% подготовится, но она хотя бы даёт неплохое представление о том, что вообще нужно знать к экзамену.
Можете также почитать достаточно подробную [рецензию на эти книжки](http://sonyks2007.blogspot.ru/2014/02/microsoft-exam-70-483.html).
А ещё, я наткнулся на замечательный блог [mypathto70-483.blogspot.ru](http://mypathto70-483.blogspot.ru/). Автор блога основательно готовится к экзамену, подробно прорабатывая каждую (даже самую небольшую) тему из экзаменационного перечня. По мере подготовки с хорошей регулярностью появляются соответствующие посты. *Update: В настоящее время автор успешно сдал экзамен*.

Хочется отметить, что если вы неплохой .NET-разработчик, если вы понимаете как работает платформа и неплохо помните стандартные классы, то можно почти не готовиться. Разве что, просмотреть те темы, с которыми вам сталкиваться не приходилось (например, криптографию, многопоточность, работу со сборками или профилирование приложений — далеко не всем по работе попадаются все тематики экзамена).

### Записываемся на экзамен

Большую часть времени я нахожусь в Новосибирске, так что экзамен решил проходить тут же. В Нск сертификацией от Microsoft занимается [Сибинфоцентр](http://www.sibinfo.ru/). Если есть какие-то непонятности, то можно туда позвонить и получить подробную консультацию по всем интересующим вас вопросом.

Непосредственную регистрацию необходимо проходить на сайте [https://www.prometric.com](https://www.prometric.com). У сайта, мягко говоря, с навигацией проблемы. Дам подсказку: чтобы записаться на экзамен, нужно сначала зарегистрироваться. А регистрация происходит по нажатию на надпись «IT TEST TAKER Account Sign-In» в центре страницы, которая умело маскируется под радиобаттон. После этого всё просто: проходите регистрацию, выбираете экзамен (категория <i>Microsoft (070, 071, 074, MBx)</i>), выбираете нужный центр тестирования и удобное для вас время. Как правило, свободного времени хватает — отчего-то особых очередей на сдачу Microsoft-овских экзаменов не наблюдается.

### Сдаём экзамен

В Сибинфоцентре мне очень понравилось. Пришёл я с хорошим временным запасом, так что мне предложили дождаться начала экзамене в местной комнате отдыха:

<p class="center">
  <img src="/img/posts/dotnet/ms-mcp/subinfocenter.png" />
</p>

Комната отдыха хороша: есть чай, кофе и вкусные печеньки. Причём, печеньки действительно вкусные — ощущается, что ребята не набрали каких попало, а именно выбирали повкуснее. Ценю я внимание к мелочам =). Также есть всякие проспектики о проводимых в центре сертификациях, так что вполне можно занять себя кофейком и полезным чтением.

Вскоре пришёл ответственный за проведение Microsoft-овского тестирования. Надо отдать ему должное — он меня хорошо проинструктировал о процессе прохождении экзамена и подробно ответил на все вопросы. В общем, ребята держат достойный уровень.

Перед экзаменом необходимо предъявить два удостоверения личности (в моём случае подошли паспорт и водительское удостоверение). Все личные вещи складываются в сейф, т.к. в экзаменационную комнату их проносить нельзя. С собой выдаётся лишь два специальных листа бумаги и два маркера (впрочем, за время экзамена мне не довелось их испытать).

Перед экзаменом я наподписывал всяких [NDA](https://www.microsoft.com/learning/en-us/certification-exam-policies.aspx), поэтому особо ничего про экзамен рассказать не могу, ограничусь лишь впечатлениями. Экзамен меня порадовал: все вопросы весьма адекватные и корректные (что особенно радует после подготовительной литературы). Было несколько вопросов, которые меня смутили, но тут скорее всего со мной что-то не так, а не с экзаменом. Тестирующая система тоже порадовала: всё понятно, пользоваться удобно, ничего не глючит — сплошное удовольствие. Перед экзаменом также можно пройти быстрый туториал по тому, как ей пользоваться, ввиду чего непонятных моментов не остаётся вовсе. Основная часть экзамена идёт два часа, но этого более, чем достаточно. Если вы всё знаете, то можно успеть прочитать каждый вопрос на несколько раз, подумать, ещё раз перечитать вопрос, ещё раз основательно подумать, выбрать правильный ответ, а по окончанию тестирования ещё раз всё проверить. А если вы что-то не знаете, то дополнительное время не особо поможет. У меня на всё вместе с проверкой ушло 1.5 часа. После окончания тестирования вам предложат написать комментарии к вопросам (они отправляются в Microsoft для улучшения качества тестирования), а также пройти короткий опросник об уровне сертификации (но это всё опционально). Предварительные результаты (а скорее всего они и будут окончательными) сразу показываются на экране.

После успешного прохождения экзамена вам на руки выдаются ваши результаты в печатном виде с рифлёной печатью и подписью, после чего предлагается ждать письма на почту. Результаты тестирования отправляются сперва в Prometrics, затем в Microsoft, где их должны аккуратно проверить в течении 7 рабочих дней. Начитавшись разных отзывов о получении MCP ([раз](http://sonyks2007.blogspot.ru/2014/02/mcp.html),	[два](http://www.danshin.ms/2007/12/mcp.html)) я опасался, что эта часть затянется. Но Microsoft меня порадовали: уже на следующий день мне пришло письмо, по ссылке из которого я зарегистировался на сайте [mcp.microsoft.com](http://mcp.microsoft.com/). После этой процедуры выдаётся учётная запись, с которой связывается свежевыданный *Micrisoft Certification ID*. Из под этой учётной записи можно в любой момент смотреть все сданные экзамены, планировать будущие экзамены и расшаривать данные о сертификатах заинтересованным лицам. Собственно говоря, после регистрации я увидел у себя две записи: *MCPS: Microsoft Certified Professional* и *MS: Programming in C# Specialist*. Выглядят они примерно так:

<p class="center">
  <img src="/img/posts/dotnet/ms-mcp/mcp.png" />
</p>

У Microsoft есть также более крутые сертификации, для получения которых нужно пройти несколько экзаменов. После 70-483 можно качаться либо в *MCSD: Windows Store Apps Using C#*, либо в *MCSD: Web Applications*. Но, увы, подобные штуки меня уже не настолько вдохновляют. Так что будем ждать, когда MS проведут очередную реорганизацию системы сертификатов, чтобы в ней появилось что-нибудь более заманчивое =).

### Выводы

Впечатления от мероприятия сугубо положительные, опыт полезный. Если вы в себе ощущаете силы пройти подобную сертификацию, а также у вас есть на это время и возможности ($80 или ваучер), то я бы посоветовал озадачиться. Как минимум, это достаточно интересно, а бумажка от MS тоже лишней не будет, можно будет включить в резюме.
