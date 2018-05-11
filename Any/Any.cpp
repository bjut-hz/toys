// http://www.cnblogs.com/feixue/p/boost-any.html
#include <typeinfo>
#include <iostream>
#include <assert.h>
class any {
public: // structors  
	any() : content(0) {}

	template<typename ValueType>
	any(const ValueType & value) : content(new holder<ValueType>(value)) {}

	any(const any & other) : content(other.content ? other.content->clone() : 0) {}
	~any() { delete content;}

public: // modifiers  

	any& swap(any & rhs) {
		std::swap(content, rhs.content);
		return *this;
	}

	template<typename ValueType>
	any& operator=(const ValueType & rhs) {
		any(rhs).swap(*this);
		return *this;
	}

	any& operator=(any rhs) {
		rhs.swap(*this);
		return *this;
	}

public: // queries  

	bool empty() const {
		return !content;
	}

	const std::type_info & type() const {
		return content ? content->type() : typeid(void);
	}

public: // types (public so any_cast can be non-friend)  
	class placeholder {
	public: // structors  
		virtual ~placeholder() {}

	public: // queries  
		virtual const std::type_info & type() const = 0;
		virtual placeholder * clone() const = 0;
	};

	template<typename ValueType>
	class holder : public placeholder {
	public: // structors  
		holder(const ValueType & value): held(value) {}
		
		// queries  
		virtual const std::type_info & type() const {
			return typeid(ValueType);
		}
		virtual placeholder * clone() const {
			return new holder(held);
		}

	public: // representation  
		ValueType held;

	private: // intentionally left unimplemented  
		holder & operator=(const holder &);
	};

public: // representation (public so any_cast can be non-friend)  

	placeholder * content;

};

template<typename ValueType>
ValueType any_cast(const any& operand) {
	assert(operand.type() == typeid(ValueType));
	return static_cast<any::holder<ValueType> *>(operand.content)->held;
}


int main() {
	any i_a = 3;
	int i_b = any_cast<int>(i_a);
	//std::cout << i_a << "\t" << i_b << std::endl;

	any s_a = std::string("demo");
	std::string s_b = any_cast<std::string>(s_a);
	//std::cout << s_a << "\t" << s_b << std::endl;

	return 0;
}