   M S B A M L   `   `   `  ����   	FBoothApp     FBoothApp.CustomMessageBox   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006       � Title$  Message��$	��350�� � AllowsTransparency . 
������ 
���   � � 
ResizeMode$ NoResize=� � SizeToContent$ WidthAndHeight=� � WindowStartupLocation$ CenterScreen=� � WindowStyle$
 None=�.���� 
����������#CCCCCC$��1q� �� CornerRadius$ 15��._�� $��10q� � RowDefinitions �� $
��Auto ��� $
��Auto �.+��� $��20q�$��Center=� �� Orientation$ 
Horizontal=�.+��� $��40��$��40��$��0,0,20,0q�$��backgrounds/warning.png���� -   	 ��Name$	 MessageText��$	��500��$��Center=�$��16(�$
��Wrap=��� $��1��$��10,10,10,10q�$��Center=�$ 
Horizontal=�.+��� $��0,0,10,0q�
 �� Padding$
 0q����#28a745$ 5��._��� -   $��80��$��30��
������ 
������ $��OK��$
��Bold"�
�����������  �� 
TargetType" ��.���  �� Property � �� �� Value
 ���� ��  � ��
 ���� ��  � ��
 �������  � ��$
 Bold"���  � ��$ 0q���  � �� ��  �� 
TargetType" ��.R��� #��z��#��z��$ 5��._��� $��Center=�$��Center=� �� Triggers P�  P� Property �@J�IsMouseOver P� Value . . ���  � �� �#28a745��  � ��
 ���� P�  � �� . . ���  � �� �#28a745��  � ��
 ���� �� $
 0q����#F44336$ 5��._��� -   $��80��$��30��
������ 
������ $��Cancel��$
��Bold"�
����������� " ��.���  � ��
 ���� ��  � ��
 ���� ��  � ��
 �������  � ��$
 Bold"���  � ��$ 0q���  � �� �� " ��.R��� #��z��#��z��$ 5��._��� $��Center=�$��Center=� P�  �@J�IsMouseOver . . ���  � �� �#F44336��  � ��
 ���� P�  � �� . . ���  � �� �#F44336��  � ��
 ���� 