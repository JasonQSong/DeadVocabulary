   M S B A M L   `   `   `  ����   WpfDeadVocabulary!    WpfDeadVocabulary.SearchBar   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     -     � Title$  	SearchBar��$	��300��$	��300�� � AllowsTransparency .  � WindowStyle$
 None=� � Topmost . 
������ .���  � RowDefinitions �� $
��Auto ��� $
��Auto ��� .+�� $��0�� � ColumnDefinitions �� .+��� -    ��Name$ textBoxSearchKey��$��0��$��3,3,3,3q�$
��20pt(�$�� �����	#80404040
���������  �� Orientation$ 
Horizontal=�$	��6,3q�$��Right=�.+��� -    ��Name$ buttonPlaySound��$��1��$��+��$��20��$��20��$��3q�$��Center=�$��Right=����� .R��� $��images/Sound2.png��$��20��$��20��$��Center=�$��Center=��� -   $ 	buttonAdd��$��1��$��+��$��20��$��20��$��3q�$��Center=�$��Right=����� .R��� $��Center=�$��Center=�
�������$��+��$
��20pt(�� -   	 �Name$	 gridDetails��$��1��$y�	Collapsed=� �� $
��Auto ��� $
��Auto �.+��� -   $ labelWordDetail��$��0��$��3,3,3,3q�
 �� 
IsReadOnly
 .  �� TextWrapping$
 Wrap=����	#80404040
��������� -    ��Name$ labelTip��$��1��$��3q�$�� �����	#80404040
�������y���  �� ElementName$ labelTip�� �� Path$
 Text� $   Text2VisibilityCollapseConverter �� 	Converter# [  