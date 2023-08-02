<?xml version="1.0"?>
<xsl:stylesheet version="1.0"
xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html"/>
  <xsl:template match="/">
    <html>
      <head>
        <title>Deploy XML document</title>
        <style type="text/css">
            <![CDATA[
          body
          {
          font-family:Arial, Helvetica, Sans-Serif;
          color:#000000;
          text-align:left;
          }
          h2{
          margin:0px;
          }
          hr
          {
          border: 0px;
          height:1px;
          border-top: solid 3px #000000;
          }
          li.comment{
          border:dotted 1px #009900;
          color:#009900;
          list-style-type:none;
          padding-left:5px;
          }
          ]]>
        </style>
      </head>
      <body>
        <h1>
          Deploy XML Document for <xsl:value-of select="deploy/metadata/productname"/>
        </h1>
        <h2>General</h2>
        <p>
          A deploy.xml document is used in combination with the <b>Deliveries_Setup.exe</b>.<br/>
          Deliveries_Setup.exe is used to install/uninstall/repair software in <b>Deliveries_Setup</b> and parses the <b>deploy.xml</b> file and does what is specified in it.
          
        </p>
        <h2>
          About the installation of <xsl:value-of select="deploy/metadata/productname"/>:
        </h2>
        <p>
          This deploy.xml document is for the installation of the product <b>
            <xsl:value-of select="deploy/metadata/productname"/>
          </b><br/>Version: <b>
            <xsl:value-of select="deploy/metadata/productversion"/>
          </b><br/> Productcode: <b>
            <xsl:value-of select="deploy/metadata/productcode"/>
          </b><br/> Manufacturer: <b>
            <xsl:value-of select="deploy/metadata/manufacturer"/>
          </b><br/>
          The packager of this installation is: <b>
            <xsl:value-of select="deploy/metadata/packager"/>
          </b><br/>he created it on <b>
            <xsl:value-of select="deploy/metadata/creationdate"/>
          </b>.<br/>The type of this installation is a <b>
            <xsl:value-of select="deploy/metadata/type"/>
          </b>.
        </p>
        <hr/>
        <ul>
          <li class="comment">Comments of the author.</li>
        </ul>
        <h2>Installation task queue:</h2>
        This queue starts if you execute Deliveries_Setup.exe with the type parameter <b>i</b>.<br/>For example: Deliveries_Setup.exe XML=Deploy_Test.xml TYPE=<b>i</b>.
        <ul>
          <xsl:apply-templates select="/deploy/install/tasks" />
        </ul>
        <hr/>
        <h2>Uninstallation tasks queue:</h2>
        This queue starts if you execute Deliveries_Setup.exe with the type parameter <b>x</b>.<br/>For example: Deliveries_Setup.exe XML=Deploy_Test.xml TYPE=<b>x</b>.
        <ul>
          <xsl:apply-templates select="/deploy/uninstall/tasks"/>
        </ul>
        <hr/>
        <h2>Repair task queue:</h2>
        This queue starts if you execute Deliveries_Setup.exe with the type parameter <b>f</b>.<br/>For example: Deliveries_Setup.exe XML=Deploy_Test.xml TYPE=<b>f</b>.
        <ul>
          <xsl:apply-templates select="/deploy/repair/tasks"/>
        </ul>
        <hr/>
      </body>
    </html>
  </xsl:template>
  <xsl:template match="abort">
    <li>
      The main task queue is aborted
      <xsl:if test="./@exitcode">
        (Exitcode: <b>
        <xsl:value-of select="./@exitcode"/>
        </b>)
      </xsl:if>
      <xsl:if test="./@message"> with the messagebox: <b>
        <xsl:value-of select="./@message"/>
      </b>
      </xsl:if>
    </li>
  </xsl:template>
  <xsl:template match="checkbattery">
    <li>
      If the computer (usually a laptop) is running in battery mode:
      <ul>
        <xsl:apply-templates select="./node()"/>
      </ul>
    </li>
  </xsl:template>
  <xsl:template match="messagebox">
    <li>
      A message box of the type <b>
        <xsl:value-of select="./@type"/>
      </b> is displayed. It's message: <b>
        <xsl:value-of select="."/>
      </b>
    </li>
  </xsl:template>
  <xsl:template match="postpone">
    <li>
      The user is asked if he wants to postpone this queue
      <xsl:if test="./@message">
        with this message: <b>
          <xsl:value-of select="./@message"/></b>
      </xsl:if>.
      <xsl:if test="./@max">
        The user has <b><xsl:value-of select="./@max"/></b> attempts to postpone this queue.
      </xsl:if>
      <xsl:choose>
        <xsl:when test="./@dialog='false'">
          The user won't have a possibility to choose a new date.
        </xsl:when>
        <xsl:otherwise>
          If so, this queue will be postponed to the date the user has choosen.
        </xsl:otherwise>
      </xsl:choose>
    </li>
  </xsl:template>
  <xsl:template match="enableinstallgui">
    <li>
      <xsl:choose>
        <xsl:when test="./@unattended='true'">
          This is a fully automatic installation queue without any user interaction.
        </xsl:when>
        <xsl:otherwise>
          This is an installation queue with user interaction.
        </xsl:otherwise>
      </xsl:choose>
    </li>
  </xsl:template>
  <xsl:template match="disableinstallgui">
    <li>
      The installtion continues without user interaction.
    </li>
  </xsl:template>
  <xsl:template match="checkfullscreen">
    <li>
      If the computer is running in fullscreen mode,
      <xsl:choose>
        <xsl:when test="./@loop='true'">
          the following tasks are repeated until the computer is not in fullscreen mode anymore:
        </xsl:when>
        <xsl:otherwise>
          the following tasks are executed one time:
        </xsl:otherwise>
      </xsl:choose>
      <ul>
        <xsl:apply-templates select="./node()"/>
      </ul>
    </li>
  </xsl:template>
  <xsl:template match="startservice">
    <li>
      starts the service <b>
        <xsl:value-of select="./@name"/>
      </b>.
      <xsl:if test="./node()">
        If the service did not start the following tasks are executed:
        <ul>
          <xsl:apply-templates select="./node()"/>
        </ul>
      </xsl:if>
    </li>
  </xsl:template>
  <xsl:template match="stopservice">
    <li>
      stops the service <b>
        <xsl:value-of select="./@name"/>
      </b>.
      <xsl:if test="./node()">
        If the service did not stop the following tasks are executed:
        <ul>
          <xsl:apply-templates select="./node()"/>
        </ul>
      </xsl:if>
    </li>
  </xsl:template>
  <xsl:template match="killprocess">
    <li>
      Kills the process <b>
        <xsl:value-of select="./@name"/>
      </b>.
    </li>
  </xsl:template>
  <xsl:template match="checkdiskspace">
    <li>
      If the drive <b>
        <xsl:value-of select="./@driveletter"/>
      </b> does not contain <b>
        <xsl:value-of select="./@space"/>
      </b> megabytes of free space:
      <ul>
        <xsl:apply-templates select="./node()"/>
      </ul>
    </li>
  </xsl:template>
  <xsl:template match="checkdisk">
    <li>
      If the 
 <xsl:choose>
        <xsl:when test="./@physical='false'">
        </xsl:when>
        <xsl:otherwise>
         (<b>physical</b> hard disk)
        </xsl:otherwise>
      </xsl:choose>      
      drive <b>
        <xsl:value-of select="./@driveletter"/>
      </b> does not contain <b>
        <xsl:value-of select="./@space"/>
      </b> megabytes of free space:
      <ul>
        <xsl:apply-templates select="./node()"/>
      </ul>
    </li>
  </xsl:template>
  <xsl:template match="checkmemory">
    <li>
      If the specified physical memory of <b>
        <xsl:value-of select="./@memory"/>
      </b> megabytes is not available.
      <ul>
        <xsl:apply-templates select="./node()"/>
      </ul>
    </li>
  </xsl:template>
  <xsl:template match="checkmsiinstallation">
    <xsl:if test="./exists">
      <li>
        If a msi installation with the product code <b>
          <xsl:value-of select="./@package"/>
        </b> is installed on the computer:
        <ul>
          <xsl:apply-templates select="./exists"/>
        </ul>
      </li>
    </xsl:if>
    <xsl:if test="./notexists">
      <li>
        If a msi installation with the product code <b>
          <xsl:value-of select="./@package"/>
        </b> is not installed on the computer:
        <ul>
          <xsl:apply-templates select="./notexists"/>
        </ul>
      </li>
    </xsl:if>
  </xsl:template>
  <xsl:template match="checkfile">
    <xsl:if test="./exists">
      <li>
        If the file <b>
          <xsl:value-of select="./@path"/>
        </b> exists:
        <xsl:choose>
          <xsl:when test="./exists/@loop='true'">(<b>loop</b> until '<xsl:value-of select="./@path"/>' doesn't exist anymore)</xsl:when>
        </xsl:choose>
        <ul>
          <xsl:apply-templates select="./exists"/>
        </ul>
      </li>
    </xsl:if>
    <xsl:if test="./notexists">
      <li>
        If the file <b>
          <xsl:value-of select="./@path"/>
        </b> does not exist:
        <xsl:choose>
          <xsl:when test="./notexists/@loop='true'">(<b>loop</b> until '<xsl:value-of select="./@path"/>' exists)</xsl:when>
        </xsl:choose>
        <ul>
          <xsl:apply-templates select="./notexists"/>
        </ul>
      </li>
    </xsl:if>
    <xsl:if test="count(./*)=0">
      <!--Check for the file <b>
        <xsl:value-of select="./@path"/>
      </b>-->
    </xsl:if>
  </xsl:template>
  <xsl:template match="checkregistry">
    <xsl:if test="./exists">
      <li>
        If the registry key <b>
          <xsl:value-of select="./@name"/>
        </b> in the path <b>
          <xsl:value-of select="./@path"/>
        </b><xsl:if test="./@value">
          with the value <b>
            <xsl:value-of select="./@value"/>
          </b>
        </xsl:if> exists:
        <xsl:choose>
          <xsl:when test="./exists/@loop='true'">(<b>loop</b> until the registry entry doens't exist anymore)</xsl:when>
        </xsl:choose>
        <ul>
          <xsl:apply-templates select="./exists"/>
        </ul>
      </li>
    </xsl:if>
    <xsl:if test="./notexists">
      <li>
        If the registry key <b>
          <xsl:value-of select="./@name"/>
        </b> in the path <b>
          <xsl:value-of select="./@path"/>
        </b><xsl:if test="./@value">
          with the value <b>
            <xsl:value-of select="./@value"/>
          </b>
        </xsl:if> does not exist:
        <xsl:choose>
          <xsl:when test="./notexists/@loop='true'">(<b>loop</b> until the registry entry exists)</xsl:when>
        </xsl:choose>
        <ul>
          <xsl:apply-templates select="./notexists"/>
        </ul>
      </li>
    </xsl:if>
  </xsl:template>
  <xsl:template match="checkprocess">
    <li>
      If the process <b>
        <xsl:value-of select="./@name"/>
      </b> is running, <xsl:choose>
        <xsl:when test="./@loop='true'">the queue repeats the following tasks until the process is not running anymore:</xsl:when>
        <xsl:otherwise>
          the queue executes the following tasks one time:
        </xsl:otherwise>
      </xsl:choose>
      <ul>
        <xsl:apply-templates select="./node()" />
      </ul>
    </li>
  </xsl:template>
  <xsl:template match="msiexec">
    <li>
      <b>
        Msiexec.exe <xsl:for-each select="./parameters/parameter">
          <xsl:text> </xsl:text>
          <xsl:choose>
            <xsl:when test="contains(.,' ')">
              &quot;<xsl:value-of select="."/>&quot;
            </xsl:when>
            <xsl:otherwise>
              <xsl:value-of select="."/>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:for-each>
      </b> is executed.
      <xsl:if test="returnvalues">
        <ul>
          <xsl:for-each select="./returnvalues/returnvalue">
            <li>
              If the return value is <b>
                <xsl:value-of select="./@value"/>:
              </b>
              <ul>
                <xsl:apply-templates select="./node()" />
              </ul>
            </li>
          </xsl:for-each>
        </ul>
      </xsl:if>
    </li>
  </xsl:template>
  <xsl:template match="execute">
    <li>
      Executes <b>
        <xsl:choose>
          <xsl:when test="contains(./@path,' ')">
            &quot;<xsl:value-of select="./@path"/>&quot;
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="./@path"/>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:for-each select="./parameters/parameter">
          <xsl:text> </xsl:text>
          <xsl:choose>
            <xsl:when test="contains(.,' ')">
              &quot;<xsl:value-of select="."/>&quot;
            </xsl:when>
            <xsl:otherwise>
              <xsl:value-of select="."/>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:for-each>
      </b>
      <xsl:if test="./@windowstyle">
        (Window style: "<b>
          <xsl:value-of select="./@windowstyle"/></b>")
      </xsl:if>
       and waits until the new process is terminated.
      <xsl:if test="returnvalues">
        <ul>
          <xsl:for-each select="./returnvalues/returnvalue">
            <li>
              If the return value is <b>
                <xsl:value-of select="./@value"/>:
              </b>
              <ul>
                <xsl:apply-templates select="./node()" />
              </ul>
            </li>
          </xsl:for-each>
        </ul>
      </xsl:if>
    </li>
  </xsl:template>
  <xsl:template match="removefile">
    <li>
      Removes the file: <b>
        <xsl:value-of select="./@path"/>
      </b>
    </li>
  </xsl:template>
  <xsl:template match="removefolder">
    <li>
      Removes the folder: <b>
        <xsl:value-of select="./@path"/>
      </b>
    </li>
  </xsl:template>
  <xsl:template match="copyfile">
    <li>
      Copies the file: <b>
        <xsl:value-of select="./@source"/>
      </b> to <b>
         <xsl:value-of select="./@destination"/>
      </b>
      <xsl:choose>
        <xsl:when test="./@overwrite">
          (overwrite if the file already exists: <b><xsl:value-of select="./@overwrite"/></b>)
        </xsl:when>
        <xsl:otherwise>
          (overwrite if the file already exists: <b>true</b>)
        </xsl:otherwise>
      </xsl:choose>
    </li>
  </xsl:template>
  <xsl:template match="copyfolder">
    <li>
      Copies the folder: <b>
        <xsl:value-of select="./@source"/>
      </b> to <b>
         <xsl:value-of select="./@destination"/>
      </b>
    </li>
  </xsl:template>
  <xsl:template match="removeregkey">
    <li>
      Removes the registry key with the path <b>
        <xsl:value-of select="./@path"/>
      </b><xsl:if test="./@name">
        and the name <b>
          <xsl:value-of select="./@name"/>
        </b>
      </xsl:if>
    </li>
  </xsl:template>
  <xsl:template match="addregkey">
    <li>
      Adds a registry key in path <b>
        <xsl:value-of select="./@path"/>
      </b> with the name <b>
        <xsl:value-of select="./@name"/>
      </b> and the value <b>
        <xsl:value-of select="./@value"/>
      </b> (Type: <b>
        <xsl:value-of select="./@type"/>
      </b>).
    </li>
  </xsl:template>
  <xsl:template match="setreboot">
    <li>
      Sets the reboot flag so that the computer reboots after the main queue has finished.
    </li>
  </xsl:template>
  <xsl:template match="sleep">
    <li>
      The queue suspends for <b>
        <xsl:value-of select="./@seconds"/>
      </b> seconds...
      <xsl:if test="./@message">
          The message <b>
            <xsl:value-of select="./@message"/>
        </b> is displayed.
      </xsl:if>
    </li>
  </xsl:template>
  <xsl:template match="startaltiristask">
    <li>
      Tries to start the Altiris task: <b><xsl:value-of select="./@name"/></b>. If the Altiris task isn't available, the queue executes the following sub tasks:
      <ul>
        <xsl:apply-templates select="./node()"/>
      </ul>
    </li>
  </xsl:template>
  <xsl:template match="checkarchitecture">
    <xsl:if test="./x64">
      <li>
        If the architecture is x64:
        <ul>
          <xsl:apply-templates select="./x64"/>
        </ul>
      </li>
    </xsl:if>
    <xsl:if test="./x86">
      <li>
        If the architecture is x86:
        <ul>
          <xsl:apply-templates select="./x86"/>
        </ul>
      </li>
    </xsl:if>
  </xsl:template>
  <xsl:template match="executenowait">
    <li>
      Executes <b>
        <xsl:choose>
          <xsl:when test="contains(./@path,' ')">
            &quot;<xsl:value-of select="./@path"/>&quot;
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="./@path"/>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:for-each select="./parameters/parameter">
          <xsl:text> </xsl:text>
          <xsl:choose>
            <xsl:when test="contains(.,' ')">
              &quot;<xsl:value-of select="."/>&quot;
            </xsl:when>
            <xsl:otherwise>
              <xsl:value-of select="."/>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:for-each>
      </b>
      <xsl:if test="./@windowstyle">
        (Window style: "<b>
          <xsl:value-of select="./@windowstyle"/></b>")
      </xsl:if>
      and immediatly continues with the next task in the queue.
    </li>
  </xsl:template>
   <xsl:template match="getoslanguage">
    <li>
      Get the current OS language code.
      <xsl:if test="languagecodes">
        <ul>
          <xsl:for-each select="./languagecodes/languagecode">
            <li>
              If the OS language code matches the language code <b><xsl:value-of select="./@value"/></b>:
              <ul>
                <xsl:apply-templates select="./node()" />
              </ul>
            </li>
          </xsl:for-each>
        </ul>
      </xsl:if>
    </li>
  </xsl:template>
  <xsl:template match="comment()">
    <li class="comment">
      <xsl:call-template name="break">
        <xsl:with-param name="text" select="."/>
      </xsl:call-template>
    </li>
  </xsl:template>
  <xsl:template name="break">
    <xsl:param name="text" select="."/>
    <xsl:choose>
      <xsl:when test="contains($text, '&#xa;')">
        <xsl:value-of select="substring-before($text, '&#xa;')"/>
        <br/>
        <xsl:call-template name="break">
          <xsl:with-param name="text" select="substring-after($text,
'&#xa;')"/>
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="$text"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
</xsl:stylesheet>