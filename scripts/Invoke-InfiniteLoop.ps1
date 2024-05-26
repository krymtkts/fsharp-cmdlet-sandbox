function Invoke-InfiniteLoop {
    [CmdletBinding()]
    param (
        [Parameter()]
        [scriptblock]
        $ScriptBlock
    )
    end {
        while ($true) {
            if ($ScriptBlock) {
                & $ScriptBlock
            }
        }
    }
}