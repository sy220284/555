import importlib.util
import pathlib
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
MODULE_PATH = ROOT / "Tools" / "run_unity_g1.py"
SPEC = importlib.util.spec_from_file_location("run_unity_g1", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


def write_results(path, cases, *, total=None, failed=0, result="Passed"):
    total = len(cases) if total is None else total
    body = "\n".join(
        f'<test-case fullname="{name}" result="{case_result}" />'
        for name, case_result in cases
    )
    path.write_text(
        f'<test-run total="{total}" passed="{max(total - failed, 0)}" failed="{failed}" result="{result}">'
        f'{body}</test-run>',
        encoding="utf-8",
    )


class UnityG1TestResultProofTests(unittest.TestCase):
    def test_accepts_nonempty_run_with_all_required_tests_passed(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            path = pathlib.Path(temp_dir) / "results.xml"
            cases = [(name, "Passed") for name in MODULE.REQUIRED_EDITMODE_TESTS]
            cases.append(("ModernRA.Tests.OtherTests.SomethingElse", "Passed"))
            write_results(path, cases)
            MODULE.verify_test_results(path)

    def test_rejects_zero_test_run_even_when_failed_is_zero(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            path = pathlib.Path(temp_dir) / "results.xml"
            write_results(path, [], total=0, failed=0, result="Passed")
            with self.assertRaisesRegex(RuntimeError, "zero test cases"):
                MODULE.verify_test_results(path)

    def test_rejects_run_that_omits_required_smoke_test(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            path = pathlib.Path(temp_dir) / "results.xml"
            cases = [(name, "Passed") for name in MODULE.REQUIRED_EDITMODE_TESTS[:-1]]
            write_results(path, cases)
            with self.assertRaisesRegex(RuntimeError, "required tests were not executed"):
                MODULE.verify_test_results(path)

    def test_rejects_required_test_that_is_not_passed(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            path = pathlib.Path(temp_dir) / "results.xml"
            cases = [(name, "Passed") for name in MODULE.REQUIRED_EDITMODE_TESTS]
            cases[-1] = (cases[-1][0], "Skipped")
            write_results(path, cases)
            with self.assertRaisesRegex(RuntimeError, "required tests did not pass"):
                MODULE.verify_test_results(path)

    def test_rejects_failed_run_before_required_test_check(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            path = pathlib.Path(temp_dir) / "results.xml"
            cases = [(name, "Passed") for name in MODULE.REQUIRED_EDITMODE_TESTS]
            write_results(path, cases, failed=1, result="Failed")
            with self.assertRaisesRegex(RuntimeError, "EditMode tests failed"):
                MODULE.verify_test_results(path)


if __name__ == "__main__":
    unittest.main()
