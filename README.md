# AutoCAD плагин IPSDendrologyDemo

======================

Проект является примером демонстрации возможностей, которые можно реализовать с помощью плагинов в AutoCAD.  
Возможностей намного больше, в данном проекте представлена лишь маленькая часть:  
1) Создание информационной модели, которая будет хранится в документе *.dwg  
2) Выставление выносок на объекты информационной модели  
3) Изменение объектов в документе *.dwg из таблицы приложения, а также отлавливание событий изменения объектов из *.dwg  
4) Изменение номеров на проставленных выносках при изменеии номер в приложении  
5) Фокусировка внимания на выбранном в таблице приложении объекте   
6) Добавление скопированных объектов в таблицу приложения из документа  
7) Удаление объекта из таблицы приложения и из документа *.dwg  

======================

## Структура проекта

```
$PROJECT_ROOT
│   # Файлы для автоматического создания пакета AutoCAD  
├── _SettingsBundle
│   # Кастомные настройки элементов WPF 
├── CustomControl
│   # Шаблонный DWG файл из которого копируются слои, блоки, стили
├── DWG
│   # Изображения, которые используются в приложении
└── Images
│   # Дополнительные файлы для работы с документов AutoCAD
└── Other
│   # Модели данных и методы по работе с ними
└── Services
│   # Стили элементов для приложения WPF
└── Styles
│   # Обработчики для рисования в реальном времени
└── UIService
│   # Интерфейсы приложения WPF
└── View
│   # ViemModel для интерфесов приложения
└── ViewModels
│   # Главынй файл приложения, вызов программы из AutoCAD начинается с этого файла
└──Program.cs
```

##  Как запустить проект после компеляции

Необходимо выбрать в настройках проекта запуск внешней программы -> выбрать путь до acad.exe вашей версии AutoCAD
![](https://github.com/BouRHooD/IPSDendrologyDemo/blob/master/_Documents/images/_1.jpg)

##  Пример работы с программой

1) Запуск через командную строку AutoCAD
![](https://github.com/BouRHooD/IPSDendrologyDemo/blob/master/_Documents/images/_0.jpg)

2) Пример приложения в новом созданном документе
![](https://github.com/BouRHooD/IPSDendrologyDemo/blob/master/_Documents/images/_2.jpg)

3) Пример добавления в документ шаблонного блока
![](https://github.com/BouRHooD/IPSDendrologyDemo/blob/master/_Documents/images/_3.jpg)

4) Пример добавления скопированного объекта в таблицу приложения из документа  
![](https://github.com/BouRHooD/IPSDendrologyDemo/blob/master/_Documents/images/_4.jpg)

5) Пример выставления выносок на объекты 
![](https://github.com/BouRHooD/IPSDendrologyDemo/blob/master/_Documents/images/_5.jpg)

6) Пример фокусировки внимания и зумирование на объекте 
![](https://github.com/BouRHooD/IPSDendrologyDemo/blob/master/_Documents/images/_6.jpg)

7) Пример изменения типа на куст с мгновенным изменением данных в документе
![](https://github.com/BouRHooD/IPSDendrologyDemo/blob/master/_Documents/images/_7.jpg)

8) Пример изменения типа на пень с мгновенным изменением данных в документе
![](https://github.com/BouRHooD/IPSDendrologyDemo/blob/master/_Documents/images/_8.jpg)


