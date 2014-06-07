KSPDIR		:= ${HOME}/ksp/KSP_linux
MANAGED		:= ${KSPDIR}/KSP_Data/Managed
GAMEDATA	:= ${KSPDIR}/GameData
APIGAMEDATA	:= ${GAMEDATA}/KSPAPIExt
PLUGINDIR	:= ${APIGAMEDATA}/Plugins

TARGETS		:= KSPAPIExtensions.dll

API_FILES := \
    DuckTyping/AmbiguousTypesException.cs \
    DuckTyping/DelegateDuckCaster.cs \
    DuckTyping/DelegateDuckProxyType.cs \
    DuckTyping/DuckProxyType.cs \
    DuckTyping/DuckTyping.cs \
    DuckTyping/EnumStringDuckCaster.cs \
    DuckTyping/FromTypeTree.cs \
    DuckTyping/GenericParameterDuckCaster.cs \
    DuckTyping/IDuckCaster.cs \
    DuckTyping/IDuckProxy.cs \
    DuckTyping/IStaticDuckCaster.cs \
    DuckTyping/InterfaceDuckCaster.cs \
    DuckTyping/InterfaceDuckProxyType.cs \
    DuckTyping/InterfaceStaticDuckCaster.cs \
    DuckTyping/ToTypeFromTypeTree.cs \
    DuckTyping/TypeKey.cs \
    PartMessage/Attributes.cs \
    PartMessage/CommonMessages.cs \
    PartMessage/PartMessageService.cs \
    PartMessage/zzImplementation.cs \
    Properties/AssemblyInfo.cs \
    Tweakables/UIPartActionChooseOption.cs \
    Tweakables/UIPartActionFloatEdit.cs \
    Tweakables/UIPartActionsExtended.cs \
    Utils/ConfigNodeUtils.cs \
    Utils/DebuggingUtils.cs \
    Utils/EngineWrapper.cs \
    Utils/KSPUtils.cs \
    Utils/MathUtils.cs \
    Utils/ParseUtils.cs \
    Utils/PhysicsDebugger.cs \
    Utils/SystemUtils.cs \
    Utils/UnityUtils.cs \
    zzVersionChecker.cs \
	$e

RESGEN2	:= resgen2
GMCS	:= gmcs
GIT		:= git
TAR		:= tar
ZIP		:= zip

all: version ${TARGETS}

.PHONY: version
version:
	@../version-gen

info:
	@echo "Modular Fuel Tanks Build Information"
	@echo "    resgen2:  ${RESGEN2}"
	@echo "    gmcs:     ${GMCS}"
	@echo "    git:      ${GIT}"
	@echo "    tar:      ${TAR}"
	@echo "    zip:      ${ZIP}"
	@echo "    KSP Data: ${KSPDIR}"
	@echo "    Plugin:   ${PLUGINDIR}"

KSPAPIExtensions.dll: ${API_FILES}
	${GMCS} -unsafe -t:library -lib:${APIEXTDATA},${MANAGED} \
		-r:Assembly-CSharp,Assembly-CSharp-firstpass,UnityEngine \
		-out:$@ $^

clean:
	rm -f ${TARGETS} AssemblyInfo.cs

install: all
	mkdir -p ${PLUGINDIR}
	cp ${TARGETS} ${PLUGINDIR}

.PHONY: all clean install
