   M S B A M L   `   `   `  ���� G  COPOPCBrowser, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullA0clr-namespace:OpenPlantOPC;assembly=OPOPCBrowserOpenPlantOPC  G COPCoreLibWPF, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;-clr-namespace:OpenPlant;assembly=OPCoreLibWPF	OpenPlant L HOpenPlantOPCAdmin, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null+clr-namespace:OpenPlantOPCOpenPlantOPC    OpenPlantOPC.MainWindow   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     IOpenPlantOPC0clr-namespace:OpenPlantOPC;assembly=OPOPCBrowser     BOPLibWPF-clr-namespace:OpenPlant;assembly=OPCoreLibWPF     ,localclr-namespace:OpenPlantOPC     -     � Title$  Open-Plant OPC�� � Icon$ Icons\OpenPlant.ico��$	��450��$	��800�� � 	Resources D  >OpenPlant.CentreMessageStateNoMessageToOpacity1Else02Converter 8  4CentreMessageStateNoMessageToOpacity1Else02Converter$  OpenPlant.BoolORMultiConverter  BoolORMultiConverter�� %   &	        &	          .��� .+�  OpenPlant.OPH2  $��Center=�$��Center=�*(Disconnected from Open-Plant OPC Service� -    �Name$ gdOpenPlantOPC��.+��� -   .@��� $��Activity Logs��.��  OpenPlant.ucLogConsole  -     Name$ 
LogConsole���� -    ��Name$ 
tiSettings��$��Settings�� � DataContext   OpenPlantOPC.SettingsVM  .��� .+��  J� Opacity ��  �� ElementName$ 
cmSettings��	 �� Path$	 State�
 �� 	Converter#
 [   � RowDefinitions �� $��* ��� $��* ��� $
��auto � � ColumnDefinitions �� �� .+�  OpenPlant.OPBorder  $��3q�$��0��$��0��._���  �� Orientation$ Vertical=�$��Center=�$��Center=�.+�  OpenPlant.OPH3  $��Center=�$��0,0,0,2q�$��0��$
��Bold"�
HTTP API�� $��1��$��25��$ 
Horizontal=�.+��� $��Center=�$��0,0,0,0q�Port:�� $��Center=�$��2,0,0,0q�$��80��{��� *EnableAPI_Http+���� *	Http_Port+�� $��Center=�$��10,0,0,0q� d� 	IsChecked �� *EnableAPI_Http+.���� $��0,1,0,0q�Enable  $��3q�$��1��$��1��._��� $ Vertical=�$��Center=�$��Center=�.+�  $��Center=�$��0,0,0,2q�$��0��$
��Bold"�BASIC AUTHENTICATION�� $��Center=�$ Vertical=�$��0,5,0,0q�{�`�  `� 	Converter# [ .1��� $	 EnableAPI_Http��� $	 EnableAPI_Https�.+��� $��Center=�$��0,0,0,10q� �� *RequireAPIBasicAuthentication+.���� $��0,1,0,0q�Enable Basic Authentication�� $ 
Horizontal=�$��20��.+��� $��60�� �� TextAlignment$ Right=�$��0,0,2,0q�	Username:�� $	��100��{��� *RequireAPIBasicAuthentication+���� *$"Username_ForAPIBasicAuthentication+�� $ 
Horizontal=�$��0,5,0,0q�$��20��.+��� $��60��$ Right=�$��0,0,2,0q�	Password:G� -    G�Name$ pbHTTPSAuthentication��$	��100��{��� *RequireAPIBasicAuthentication+  $��3q�$��0��$��1��._��� $ Vertical=�$��Center=�$��Center=�.+�  $��Center=�$��0,0,0,2q�$��0��$
��Bold"�HTTPS SECURE API�� $��25��$��Center=�$ 
Horizontal=�.+��� $��Center=�$��0,0,0,0q�Port:�� $��Center=�$��2,0,0,0q�$��80��{��� *EnableAPI_Https+���� *
Https_Port+�� $��Center=�$��10,0,0,0q� �� *EnableAPI_Https+.���� $��0,1,0,0q�Enable�� $��Center=�$��0,10,0,0q�*(Subject Alternative Names (for SSL Cert)�� $��Center=�$��0,2,0,10q�{��� *EnableAPI_Https+���� *&$SubjectAlternativeNames_ForHTTPSCert+  $��3q�$��1��$��0��._��� $ Vertical=�$��Center=�$��Center=�.+�  $��Center=�$��0��$��0,0,0,2q�$
��Bold"�OPC Settings�� $��22��$ 
Horizontal=�.+��� $��Center=�$ Right=�$	��200��$��0,0,0,0q�&$OPC Classic Subscription Size Limit:�� $��Center=�$��2,0,0,0q�$��80������ *'%OPCClassic_SubscriptionGroupSizeLimit+�� $��22��$ 
Horizontal=�.+��� $��Center=�$ Right=�$	��200��$��0,0,0,0q�!OPC UA Subscription Size Limit:�� $��Center=�$��2,0,0,0q�$��80������ *" OPCUA_SubscriptionGroupSizeLimit+  $��3q�$��45��$��2��$��2��._�  OpenPlant.OPButton  -   $��5q�$
��auto��.��Save and Apply	  OpenPlant.CentreMessage	  -    	 Name$ 
cmSettings��$y�	Collapsed=� 	  MessageProcess$ 	Saving...�� 	  MessageSuccess$ Save Successful!��# 	  ShowDesignTimeMessageProcess .  �� $��Test Browse OPC UA��.��
   OpenPlantOPC.OPCBrowser
  -	    
 Name$ OPCUA_Browser�� 
  OPCBrowserType$ OPCUA=�$��1q����#ccc 
  ShowAddedCount .  
  AllowAddConnection .  
  ShowCheckBoxes .  
  ShowCancelButton .   
  EnableBulkSelectionMode .  
  BrowseForOPCServersOnly .  �� $��Test Browse OPC Classic��.��
  -
   $ OPCClassic_Browser��$ 
OPCClassic=�$��1q����#ccc .  .  .  .   .  .  