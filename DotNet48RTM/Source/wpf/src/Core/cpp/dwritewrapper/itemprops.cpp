#include "ItemProps.h"

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    ///<SecurityNote>
    /// Critical    - Exposes a native pointer.
    ///</SecurityNote>
    [SecurityCritical]
    void* ItemProps::ScriptAnalysis::get()
    {
        if (_scriptAnalysis != nullptr)
        {
            return _scriptAnalysis->Value;
        }
        else
        {
            return NULL;
        }
    }

    ///<SecurityNote>
    /// Critical    - Exposes a native pointer.
    ///</SecurityNote>
    [SecurityCritical]
    void* ItemProps::NumberSubstitutionNoAddRef::get()
    {
        return _numberSubstitution->Value;
    }

    CultureInfo^ ItemProps::DigitCulture::get()
    {
        return _digitCulture;
    }

    bool ItemProps::HasExtendedCharacter::get()
    {
        return _hasExtendedCharacter;
    }

    bool ItemProps::NeedsCaretInfo::get()
    {
        return _needsCaretInfo;
    }

    bool ItemProps::HasCombiningMark::get()
    {
        return _hasCombiningMark;
    }
    
    bool ItemProps::IsIndic::get()
    {
        return _isIndic;
    }

    bool ItemProps::IsLatin::get()
    {
        return _isLatin;
    }

    ///<SecurityNote>
    /// Critical    - Asserts to allocate and initialize unmanaged memory.
    /// TreatAsSafe - Initializes unmanaged memory to known safe state.
    ///</SecurityNote>
    ItemProps::ItemProps()
    {
        _digitCulture           = nullptr;
        _hasCombiningMark       = false;
        _hasExtendedCharacter   = false;
        _needsCaretInfo         = false;
        _isIndic                = false;
        _isLatin                = false;

        _numberSubstitution     = nullptr;
        _scriptAnalysis         = nullptr;
          
    }

    ///<SecurityNote>
    /// Critical    - Sets members to arbitrary native pointers that are later read by treat as safe methods.
    ///</SecurityNote>
    [SecurityCritical]
    [SecurityPermission(SecurityAction::Assert, UnmanagedCode = true)] 
    ItemProps^ ItemProps::Create(
        void* scriptAnalysis,
        void* numberSubstitution,
        CultureInfo^ digitCulture,
        bool hasCombiningMark,
        bool needsCaretInfo,
        bool hasExtendedCharacter,
        bool isIndic,
        bool isLatin
        )
    {
        ItemProps^ result = gcnew ItemProps();

        result->_digitCulture          = digitCulture;
        result->_hasCombiningMark      = hasCombiningMark;
        result->_hasExtendedCharacter  = hasExtendedCharacter;
        result->_needsCaretInfo        = needsCaretInfo;
        result->_isIndic               = isIndic;
        result->_isLatin               = isLatin;

        if(scriptAnalysis != NULL)
        {
            result->_scriptAnalysis           = gcnew NativePointerWrapper<DWRITE_SCRIPT_ANALYSIS>(new DWRITE_SCRIPT_ANALYSIS());
            *(result->_scriptAnalysis->Value) = *((DWRITE_SCRIPT_ANALYSIS*)scriptAnalysis);
        }

        IDWriteNumberSubstitution* tempNumSubstitution = (IDWriteNumberSubstitution*)numberSubstitution;
        if(tempNumSubstitution != NULL)
        {
            tempNumSubstitution->AddRef();
        }
            
        result->_numberSubstitution = gcnew NativeIUnknownWrapper<IDWriteNumberSubstitution>(tempNumSubstitution);

        return result;
    }

    ///<SecurityNote>
    /// Critical - Asserts to read unmanaged memory.
    /// Safe     - Reads from a safe location.
    ///</SecurityNote>
    [SecuritySafeCritical]
    [SecurityPermission(SecurityAction::Assert, UnmanagedCode = true)] 
    __declspec(noinline) bool ItemProps::CanShapeTogether(ItemProps^ other)
    {
        // Check whether 2 ItemProps have the same attributes that impact shaping so
        // it is possible to shape them together.
        bool canShapeTogether =  (this->_numberSubstitution->Value == other->_numberSubstitution->Value // They must have the same number substitution properties.
            && 
            ((this->_scriptAnalysis == other->_scriptAnalysis)
            ||
            (this->_scriptAnalysis != nullptr && other->_scriptAnalysis != nullptr &&
             this->_scriptAnalysis->Value->script == other->_scriptAnalysis->Value->script  &&         // They must have the same DWRITE_SCRIPT_ANALYSIS.
             this->_scriptAnalysis->Value->shapes == other->_scriptAnalysis->Value->shapes)));

        GC::KeepAlive(other);
        GC::KeepAlive(this);
        return canShapeTogether;
    }
}}}}//MS::Internal::Text::TextInterface
