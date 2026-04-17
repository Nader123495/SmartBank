$wmiNamespace = Get-WmiObject -Namespace "root\Microsoft\SqlServer" -Class __NAMESPACE | Where-Object {$_.Name -like "ComputerManagement*"} | Sort-Object Name -Descending | Select-Object -First 1
if ($wmiNamespace) {
    $ns = "root\Microsoft\SqlServer\" + $wmiNamespace.Name
    $tcpProps = Get-WmiObject -Namespace $ns -Class ServerNetworkProtocolProperty | Where-Object { $_.InstanceName -eq "SQLEXPRESS" -and $_.ProtocolName -eq "Tcp" -and $_.IPAddressName -eq "IPAll" }
    foreach ($prop in $tcpProps) {
        if ($prop.PropertyName -eq "TcpPort") {
            $prop.SetStringValue("1433") | Out-Null
        }
        if ($prop.PropertyName -eq "TcpDynamicPorts") {
            $prop.SetStringValue("") | Out-Null
        }
    }
}
