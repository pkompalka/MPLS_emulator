   M S B A M L   `   `   `  ����?  ;Host, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullclr-namespace:HostHost  5   	   H DMahApps.Metro, Version=1.6.5.1, Culture=neutral, PublicKeyToken=nullV;clr-namespace:MahApps.Metro.Controls;assembly=MahApps.MetroMahApps.Metro.Controls 5   	       Host.HostWindow   5      W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   $localclr-namespace:Host     5   	   PControls;clr-namespace:MahApps.Metro.Controls;assembly=MahApps.Metro     5   	     � 
ResizeMode$  CanMinimize=�5	   	    � Title$ 
MainWindow��5
   	   $	��100��6   $	��410��6)   .��5      � $��0,0,-0.4,0.4q�6   .+�5   
   �� $��Wiadomość:��6   $��110,0,214.6,52q�6'   �� 5   
   $��Gdzie wysłać:��6   $��108,41,214.6,9q�6+   �� 5   
   $��	Okres[s]:��6   $��0,0,331.6,51q�6&   �� 5   
   $��63,0,306.6,51q�66   ��6   ��  �� Path$ 
PeriodText�66   �� 5   
   -   5         ��Name$ messageText��5   8   
�������
���   �$��194,0,0.6,44q�6M   �� 5   
   -   5         ��Name$ 
fastButton��5      
���E��$	��0,5��6>   $��6,37,369.6,3q�6e   �� 5   
   -   5        $ normalButton��5      
���� ��$��1��6;   $��39,37,337.6,3q�6b   �� 5   
   -   5        $ 
slowButton��5      
��� ���$��5��69   $��71,37,306.6,3q�6^   �� 5   
   -   5         ��Name$ 
whereCombo��5      $��194,41,121.6,3q�6'   $ �� IsSynchronizedWithCurrentItem$ FalseY�6?   6e   �� 5   
   -   5        $ 
sendButton��5      $��35��6%   $��35��60   
��� � �   MetroCircleButtonStyle#��[  6h   $��301,35,67.6,0q�6�   .��5      �� $��15��6   $��15��6#   ��6/   �� $ 
Foreground� �� RelativeSource �� *FindAncestor+ �� AncestorType" ��	 J� OpacityMask	 5      !� 5      
 j� Stretch$

 Fill=�6"     appbar_chevron_right !� Visual# �  61   5      5      �� 5   
   -   5        $ 
stopButton��5      $��35��6%   $��35��60   
���  ��#��[  6f   $��358,35,10.6,0q�6�   .��5      �� $��20��6   $��20��6#   ��6/   �� $ 
Foreground� �� *FindAncestor+" ��	 5      !� 5      $

 Fill=�6"     appbar_close# �  61   5!      5"      5#      5$      