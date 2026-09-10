import { SignupPanel } from '../../components/signup-panel';

export default function OnboardingPage() {
  const steps = ['بيانات الشركة', 'قالب دليل الحسابات', 'أول فرع/مستودع/خزنة', 'دعوة مدير', 'هل تريد الاستيراد من النظام القديم؟'];
  return <div className="grid"><section className="hero"><h1>معالج تهيئة المستأجر</h1><p>أنشئ منشأتك وحساب المالك في خطوة واحدة — تُجهَّز لك الحسابات الافتراضية والفرع الرئيسي والمستودع والخزنة تلقائياً.</p></section><section className="card"><ol>{steps.map((step) => <li key={step}>{step}</li>)}</ol></section><section className="card"><h2>اشترك الآن</h2><SignupPanel /></section></div>;
}
